using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace EVESyncTool.Core.Utils
{
    /// <summary>
    /// EVE配置文件夹查找
    /// </summary>
    public class FolderFinder
    {
        private readonly Dictionary<string, string> _serverKeywords;
        private readonly Action<string, string, string> _logAction;
        private readonly Action<string> _onFolderFound;

        public FolderFinder(Dictionary<string, string> serverKeywords, Action<string, string, string> logAction, Action<string> onFolderFound)
        {
            _serverKeywords = serverKeywords;
            _logAction = logAction;
            _onFolderFound = onFolderFound;
        }

        /// <summary>
        /// 自动查找文件夹（先查缓存，再快速查找）
        /// </summary>
        public string AutoFind(string serverName, string cachedPath, Action updateUi = null)
        {
            updateUi?.Invoke();

            if (cachedPath != null && cachedPath.ToLower().Contains(_serverKeywords[serverName]))
            {
                if (Directory.Exists(cachedPath))
                {
                    _logAction?.Invoke("自动查找文件夹", "缓存命中", cachedPath);
                    return cachedPath;
                }
            }

            string found = QuickFind(serverName);

            if (found != null)
            {
                _logAction?.Invoke("自动查找文件夹", "成功", found);
                _onFolderFound?.Invoke(found);
                return found;
            }

            _logAction?.Invoke("自动查找文件夹", "失败", "未找到设置文件夹");
            return null;
        }

        /// <summary>
        /// 快速查找（优先 LOCALAPPDATA，再 C/D/E 盘）
        /// </summary>
        public string QuickFind(string serverName)
        {
            if (!_serverKeywords.TryGetValue(serverName, out string keyword))
                return null;

            _logAction?.Invoke("快速查找", $"查找 {serverName} 配置", "");

            // ===== 1. 优先查找 %LOCALAPPDATA%\CCP\EVE\ =====
            string localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(localAppData))
            {
                string evePath = Path.Combine(localAppData, "CCP", "EVE");
                if (Directory.Exists(evePath))
                {
                    _logAction?.Invoke("快速查找", $"检查 LOCALAPPDATA: {evePath}", "");
                    string folder = ScanDirectory(evePath, keyword);
                    if (folder != null)
                    {
                        _logAction?.Invoke("快速查找", "成功", folder);
                        return folder;
                    }
                }
            }

            // ===== 2. 备用：C/D/E 盘 =====
            string[] drives = { "C:", "D:", "E:" };
            foreach (string drive in drives)
            {
                if (!Directory.Exists(drive)) continue;

                // 2.1 检查 Users\用户名\AppData\Local\CCP\EVE\
                string userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? $"C:\\Users\\{Environment.UserName}";
                string standardPath = Path.Combine(userProfile, "AppData", "Local", "CCP", "EVE");
                if (Directory.Exists(standardPath))
                {
                    string folder = ScanDirectory(standardPath, keyword);
                    if (folder != null)
                    {
                        _logAction?.Invoke("快速查找", "成功", folder);
                        return folder;
                    }
                }

                // 2.2 检查其他可能路径
                string[] possiblePaths = {
                    Path.Combine(drive, "CCP"),
                    Path.Combine(drive, "Program Files", "CCP"),
                    Path.Combine(drive, "Program Files (x86)", "CCP"),
                    Path.Combine(drive, "Games", "EVE"),
                    Path.Combine(drive, "EVE"),
                    Path.Combine(drive, "Users")
                };
                foreach (string basePath in possiblePaths)
                {
                    if (Directory.Exists(basePath))
                    {
                        string folder = ScanDirectory(basePath, keyword);
                        if (folder != null)
                        {
                            _logAction?.Invoke("快速查找", "成功", folder);
                            return folder;
                        }
                    }
                }

                // 2.3 根目录下扫描
                try
                {
                    var directories = Directory.GetDirectories(drive, "*", SearchOption.TopDirectoryOnly);
                    foreach (string dir in directories)
                    {
                        if (dir.Count(c => c == '\\') - drive.Count(c => c == '\\') > 2) continue;
                        if (dir.ToLower().Contains(keyword.ToLower()))
                        {
                            string settingsPath = Path.Combine(dir, "settings_Default");
                            if (Directory.Exists(settingsPath))
                            {
                                _logAction?.Invoke("快速查找", "成功", settingsPath);
                                return settingsPath;
                            }
                        }
                    }
                }
                catch (UnauthorizedAccessException) { continue; }
                catch (Exception) { continue; }
            }

            return null;
        }

        /// <summary>
        /// 扫描目录
        /// </summary>
        public string ScanDirectory(string basePath, string keyword)
        {
            if (!Directory.Exists(basePath)) return null;
            try
            {
                var dirs = Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories);
                foreach (string dir in dirs)
                {
                    if (dir.ToLower().Contains(keyword.ToLower()))
                    {
                        string settingsPath = Path.Combine(dir, "settings_Default");
                        if (Directory.Exists(settingsPath))
                            return settingsPath;
                    }
                }
            }
            catch (Exception) { }
            return null;
        }

        /// <summary>
        /// 深度搜索（全盘扫描）
        /// </summary>
        public string DeepSearch(string serverName, IProgress<string> progress, Func<bool> isCancelled)
        {
            if (!_serverKeywords.TryGetValue(serverName, out string keyword))
                return null;

            DriveInfo[] drives = DriveInfo.GetDrives();

            string[] skipPaths = {
                "Windows", "System32", "Program Files", "Program Files (x86)",
                "ProgramData", "System Volume Information", "$Recycle.Bin",
                "Temp", "tmp", "Cache", "Microsoft", "MSBuild", "Reference Assemblies"
            };

            string[] priorityPaths = { "Games", "Game", "EVE", "CCP", "Program Files", "Program Files (x86)" };

            // 优先搜索 LOCALAPPDATA
            string localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(localAppData))
            {
                string evePath = Path.Combine(localAppData, "CCP", "EVE");
                if (Directory.Exists(evePath))
                {
                    progress?.Report($"搜索 LOCALAPPDATA: {evePath}");
                    string result = SearchDirectoryDeep(evePath, keyword, 5, skipPaths);
                    if (result != null) return result;
                }
            }

            foreach (DriveInfo drive in drives)
            {
                if (!drive.IsReady) continue;
                if (drive.DriveType != DriveType.Fixed) continue;

                string driveName = drive.Name;
                progress?.Report($"正在搜索 {driveName}...");

                foreach (string priorityPath in priorityPaths)
                {
                    string fullPath = Path.Combine(driveName, priorityPath);
                    if (Directory.Exists(fullPath))
                    {
                        string result = SearchDirectoryDeep(fullPath, keyword, 3, skipPaths);
                        if (result != null) return result;
                    }
                }

                try
                {
                    var directories = Directory.GetDirectories(driveName);
                    foreach (string dir in directories)
                    {
                        if (isCancelled != null && isCancelled()) return null;

                        string dirName = Path.GetFileName(dir);
                        if (skipPaths.Any(skip => dirName.Contains(skip))) continue;

                        string result = SearchDirectoryDeep(dir, keyword, 3, skipPaths);
                        if (result != null) return result;
                    }
                }
                catch (UnauthorizedAccessException) { continue; }
                catch (Exception) { continue; }
            }

            return null;
        }

        private string SearchDirectoryDeep(string basePath, string keyword, int maxDepth, string[] skipPaths)
        {
            if (maxDepth <= 0) return null;

            try
            {
                foreach (string dir in Directory.GetDirectories(basePath))
                {
                    string dirName = Path.GetFileName(dir);

                    if (skipPaths.Any(skip => dirName.Contains(skip))) continue;

                    if (dirName.ToLower().Contains(keyword.ToLower()))
                    {
                        string settingsPath = Path.Combine(dir, "settings_Default");
                        if (Directory.Exists(settingsPath))
                        {
                            return settingsPath;
                        }
                    }

                    string result = SearchDirectoryDeep(dir, keyword, maxDepth - 1, skipPaths);
                    if (result != null) return result;
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (Exception) { }

            return null;
        }

        /// <summary>
        /// 手动选择文件夹
        /// </summary>
        public string ManualSelect(IWin32Window owner)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择EVE配置文件夹 (settings_Default)";
            if (dialog.ShowDialog(owner) == DialogResult.OK)
            {
                string folder = dialog.SelectedPath;
                if (Directory.GetFiles(folder).Any(f => Regex.IsMatch(Path.GetFileName(f), @"^core_user_\d+\.dat$")))
                {
                    return folder;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取默认配置路径（当前服务器下的 settings_Default）
        /// </summary>
        public string GetDefaultPath(string currentFolder)
        {
            if (string.IsNullOrEmpty(currentFolder)) return null;
            string parentDir = Directory.GetParent(currentFolder)?.FullName;
            if (string.IsNullOrEmpty(parentDir)) return null;
            string defaultPath = Path.Combine(parentDir, "settings_Default");
            return Directory.Exists(defaultPath) ? defaultPath : null;
        }

        /// <summary>
        /// 验证文件夹是否有效
        /// </summary>
        public bool IsValidFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return false;
            return Directory.GetFiles(folder).Any(f => Regex.IsMatch(Path.GetFileName(f), @"^core_user_\d+\.dat$"));
        }
    }
}