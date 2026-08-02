using EVESyncTool.Core.Config;
using EVESyncTool.Core.Mapping;
using EVESyncTool.Core.Marshal;
using EVESyncTool.Core.Services.File;
using EVESyncTool.Data;
using EVESyncTool.Dialogs.Common;
using EVESyncTool.Dialogs.Progress;
using EVESyncTool.Dialogs.Sync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESyncTool.Core.Services.Sync
{
    public class SyncService
    {
        private readonly FileSyncManager _fileSyncManager;
        private FieldMappingService _fieldMappingService;
        private readonly MarshalSyncService _marshalService;
        private readonly ConfigManager _configManager;
        private readonly Action<string, string, string> _logAction;

        public SyncService(
            FileSyncManager fileSyncManager = null,
            FieldMappingService fieldMappingService = null,
            MarshalSyncService marshalService = null,
            ConfigManager configManager = null,
            Action<string, string, string> logAction = null)
        {
            _fileSyncManager = fileSyncManager ?? new FileSyncManager();
            _fieldMappingService = fieldMappingService ?? new FieldMappingService(new SyncSettings());
            _marshalService = marshalService ?? new MarshalSyncService();
            _configManager = configManager ?? new ConfigManager();
            _logAction = logAction;

            LoadSettings();
        }

        public void LoadSettings()
        {
            var settings = _configManager.GetSyncSettings();
            _fieldMappingService = new FieldMappingService(settings);
        }

        public SyncSettings GetSettings()
        {
            return _fieldMappingService.GetSettings();
        }

        public void SaveSettings(SyncSettings settings)
        {
            _configManager.SaveSyncSettings(settings);
            _fieldMappingService = new FieldMappingService(settings);
        }

        #region 文件同步

        public void FullSyncFolder(string sourceFolder, string targetFolder)
        {
            Log($"开始完整同步", "源", sourceFolder);
            _fileSyncManager.FullSync(sourceFolder, targetFolder, msg => Log("同步", msg, ""));
            Log($"完整同步完成", "目标", targetFolder);
        }

        public async Task<bool> SyncSingleFileAsync(string sourcePath, string targetPath)
        {
            Log($"同步文件", "源", sourcePath);
            var result = await _fileSyncManager.SyncSingleFileAsync(sourcePath, targetPath, msg => Log("同步", msg, ""));
            Log($"同步文件完成", "结果", result ? "成功" : "失败");
            return result;
        }

        /// <summary>
        /// 同步所有文件（完整覆盖 / 部分覆盖）
        /// </summary>
        public async Task SyncAllFilesAsync(
            Func<string> getCurrentFolder,
            Action<string> setCurrentFolder,
            Func<Task> refreshFileList,
            Action<string> logAction)
        {
            string currentFolder = getCurrentFolder?.Invoke();
            if (string.IsNullOrEmpty(currentFolder) || !Directory.Exists(currentFolder))
            {
                CustomMessageBox.Show("请先选择EVE配置文件夹", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 获取所有用户文件和角色文件
            var userFiles = GetFilesByPattern(currentFolder, @"^core_user_\d+\.dat$")
                .Where(f => !f.StartsWith("core_user_.dat")).ToList();
            var charFiles = GetFilesByPattern(currentFolder, @"^core_char_\d+\.dat$")
                .Where(f => !f.StartsWith("core_char_.dat")).ToList();

            // 过滤异常文件
            userFiles = userFiles.Where(f => !IsAbnormalFileName(f)).ToList();
            charFiles = charFiles.Where(f => !IsAbnormalFileName(f)).ToList();

            if (userFiles.Count <= 1 && charFiles.Count <= 1)
            {
                CustomMessageBox.Show("没有足够的文件进行覆盖操作\n每个类型至少需要2个文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // ★★★ 修复：使用 System.IO.File.GetLastWriteTime ★★★
            var latestUser = userFiles.OrderByDescending(f => System.IO.File.GetLastWriteTime(Path.Combine(currentFolder, f))).FirstOrDefault();
            var latestChar = charFiles.OrderByDescending(f => System.IO.File.GetLastWriteTime(Path.Combine(currentFolder, f))).FirstOrDefault();

            var settings = _configManager.GetSyncSettings();

            if (settings.SelectedPartialSettings == null || settings.SelectedPartialSettings.Count == 0)
            {
                settings.SelectedPartialSettings = SettingMapping.GetAll().Select(s => s.DisplayName).ToList();
                _configManager.SaveSyncSettings(settings);
            }

            logAction?.Invoke($"开始覆盖操作 - 使用保存的设置，共 {settings.SelectedPartialSettings.Count} 类");

            bool isFullSync = settings.SelectedPartialSettings.Count >= SettingMapping.GetAll().Count;

            if (isFullSync)
            {
                logAction?.Invoke("开始覆盖操作 - 完整覆盖");
                try
                {
                    _fileSyncManager.FullSync(currentFolder, currentFolder, msg => logAction?.Invoke($"同步: {msg}"));
                    await refreshFileList?.Invoke();
                    CustomMessageBox.Show("完整覆盖完成！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (IOException ioEx) when (ioEx.Message.Contains("被占用") || ioEx.Message.Contains("used"))
                {
                    logAction?.Invoke($"完整覆盖失败: 文件被占用");
                    CustomMessageBox.Show(
                        $"覆盖失败！\n\n部分文件被其他程序占用（可能是 EVE 客户端正在运行）。\n请关闭所有 EVE 客户端后重试。\n\n{ioEx.Message}",
                        "文件被占用",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    logAction?.Invoke($"完整覆盖失败: {ex.Message}");
                    CustomMessageBox.Show($"覆盖失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // ★★★ 部分覆盖 - 暂时注释掉，全部走完整覆盖 ★★★
                logAction?.Invoke($"未全选设置（当前 {settings.SelectedPartialSettings.Count}/{SettingMapping.GetAll().Count} 类），临时切换到完整覆盖");

                try
                {
                    _fileSyncManager.FullSync(currentFolder, currentFolder, msg => logAction?.Invoke($"同步: {msg}"));
                    await refreshFileList?.Invoke();
                    CustomMessageBox.Show($"完整覆盖完成！\n（提示：部分覆盖功能暂时禁用，已自动切换为完整覆盖）",
                        "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (IOException ioEx) when (ioEx.Message.Contains("被占用") || ioEx.Message.Contains("used"))
                {
                    logAction?.Invoke($"完整覆盖失败: 文件被占用");
                    CustomMessageBox.Show(
                        $"覆盖失败！\n\n部分文件被其他程序占用（可能是 EVE 客户端正在运行）。\n请关闭所有 EVE 客户端后重试。\n\n{ioEx.Message}",
                        "文件被占用",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    logAction?.Invoke($"完整覆盖失败: {ex.Message}");
                    CustomMessageBox.Show($"覆盖失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 判断是否为异常文件名
        /// </summary>
        private bool IsAbnormalFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return true;

            string[] abnormalPatterns = {
                "('char'", "('user'", "None", "dat')",
                "('", "')", ".dat.dat", ".."
            };

            foreach (var pattern in abnormalPatterns)
            {
                if (fileName.Contains(pattern))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 部分覆盖 - 暂时禁用，保留方法但不调用
        /// </summary>
        private async Task ExecutePartialSync(
            string currentFolder,
            string latestUser,
            string latestChar,
            List<string> selectedSettings,
            Action<string> logAction)
        {
            logAction?.Invoke("部分覆盖功能暂时禁用，请使用完整覆盖");
            await Task.CompletedTask;
        }

        #endregion

        #region 部分覆盖同步

        public async Task<bool> ApplyPartialOverwriteAsync(
            string sourceDatPath,
            string targetDatPath,
            List<SettingItem> selectedSettings)
        {
            if (selectedSettings == null || selectedSettings.Count == 0)
            {
                Log("部分覆盖", "错误", "未选择任何设置项");
                return false;
            }

            Log($"部分覆盖开始", "源", Path.GetFileName(sourceDatPath));
            Log($"部分覆盖开始", "目标", Path.GetFileName(targetDatPath));
            Log($"部分覆盖", "设置项数量", selectedSettings.Count.ToString());

            var result = await _fileSyncManager.ApplyPartialOverwriteAsync(
                sourceDatPath,
                targetDatPath,
                selectedSettings,
                msg => Log("部分覆盖", msg, ""));

            Log($"部分覆盖完成", "结果", result ? "成功" : "失败");
            return result;
        }

        public async Task<bool> ApplyPartialOverwriteWithCurrentSettingsAsync(
            string sourceDatPath,
            string targetDatPath)
        {
            var settings = _fieldMappingService.GetSettings();
            var selectedSettings = GetSelectedSettingsFromCurrentSettings(settings);
            return await ApplyPartialOverwriteAsync(sourceDatPath, targetDatPath, selectedSettings);
        }

        #endregion

        #region Marshal 相关

        public void DecodeDatToJson(string datPath, string jsonPath)
        {
            Log("Marshal解码", "源", datPath);
            _marshalService.DecodeToFile(datPath, jsonPath);
            Log("Marshal解码", "输出", jsonPath);
        }

        public void EncodeJsonToDat(string jsonPath, string datPath)
        {
            Log("Marshal编码", "源", jsonPath);
            _marshalService.EncodeFromFile(jsonPath, datPath);
            Log("Marshal编码", "输出", datPath);
        }

        public UserFieldMapping LoadMappingsFromDat(string datPath)
        {
            Log("加载映射", "文件", datPath);
            var mapping = _marshalService.LoadUserMappingsFromDat(datPath);
            Log("加载映射", "完成", "用户文件");
            return mapping;
        }

        #endregion

        #region 备份管理

        public string CreateBackup(string sourceFolder)
        {
            Log("创建备份", "源", sourceFolder);
            var backupPath = _fileSyncManager.BackupFolder(sourceFolder, msg => Log("备份", msg, ""));
            Log("创建备份", "路径", backupPath);
            return backupPath;
        }

        public List<BackupFolderInfo> GetBackups()
        {
            return _fileSyncManager.GetBackupFolders();
        }

        public int DeleteAllBackups()
        {
            Log("删除备份", "开始", "");
            var count = _fileSyncManager.DeleteAllBackups(msg => Log("删除备份", msg, ""));
            Log("删除备份", "完成", $"删除了 {count} 个备份");
            return count;
        }

        #endregion

        #region 设置项获取

        public Dictionary<string, List<SettingItem>> GetAllSettingItemsByCategory()
        {
            return SettingMapping.GetByCategory();
        }

        public List<SettingItem> GetAllSettingItems()
        {
            return SettingMapping.GetAll();
        }

        public List<SettingItem> GetSelectedSettingsFromCurrentSettings(SyncSettings settings)
        {
            var allItems = SettingMapping.GetAll();
            var selected = new List<SettingItem>();

            if (settings.SelectedPartialSettings == null || settings.SelectedPartialSettings.Count == 0)
            {
                return allItems;
            }

            foreach (var item in allItems)
            {
                if (settings.SelectedPartialSettings.Contains(item.DisplayName))
                {
                    selected.Add(item);
                }
            }
            return selected;
        }

        public List<SettingItem> GetSelectedSettingsFromCurrent()
        {
            var settings = _fieldMappingService.GetSettings();
            return GetSelectedSettingsFromCurrentSettings(settings);
        }

        #endregion

        #region 过滤和判断

        public void RefreshPublicChannelCache()
        {
            _fieldMappingService.RefreshPublicChannelNames();
            Log("刷新缓存", "公共频道", $"已刷新 {_fieldMappingService.GetPublicChannelNames().Count} 个频道");
        }

        public HashSet<string> GetPublicChannelNames()
        {
            return _fieldMappingService.GetPublicChannelNames();
        }

        public bool IsPrivateChat(string title)
        {
            return _fieldMappingService.IsPrivateChat(title);
        }

        public bool IsLocalChannel(string title)
        {
            return _fieldMappingService.IsLocalChannel(title);
        }

        public string ExtractBaseName(string title)
        {
            return _fieldMappingService.ExtractBaseName(title);
        }

        #endregion

        #region 工具方法

        private List<string> GetFilesByPattern(string folder, string pattern)
        {
            var files = new List<string>();
            if (!Directory.Exists(folder))
                return files;

            foreach (string file in Directory.GetFiles(folder))
            {
                string name = Path.GetFileName(file);
                if (Regex.IsMatch(name, pattern))
                    files.Add(name);
            }
            return files;
        }

        #endregion

        #region 日志辅助

        private void Log(string operation, string status, string details)
        {
            _logAction?.Invoke(operation, status, details);
        }

        #endregion
    }
}