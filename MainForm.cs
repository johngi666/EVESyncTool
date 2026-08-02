using EVESyncTool.Core;
using EVESyncTool.Core.Config;
using EVESyncTool.Core.Mapping;
using EVESyncTool.Core.Services;
using EVESyncTool.Core.Services.Backup;
using EVESyncTool.Core.Services.File;
using EVESyncTool.Core.Services.Folder;
using EVESyncTool.Core.Services.Log;
using EVESyncTool.Core.Services.Sync;
using EVESyncTool.Core.Services.UI;
using EVESyncTool.Core.UI;
using EVESyncTool.Core.Utils;
using EVESyncTool.Dialogs;
using EVESyncTool.Dialogs.Common;
using EVESyncTool.Dialogs.Config;
using EVESyncTool.Dialogs.Info;
using EVESyncTool.Dialogs.Progress;
using EVESyncTool.Dialogs.Sync;
using EVESyncTool.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESyncTool
{
    public partial class MainForm : Form
    {
        private readonly ConfigManager _configManager;
        private readonly LogService _logService;
        private readonly FolderService _folderService;
        private readonly FileListRefreshService _fileListRefreshService;
        private readonly BackupService _backupService;
        private readonly SyncService _syncService;
        private readonly DataGridViewHandler _dataGridViewHandler;
        private readonly ServerStatusManager _serverStatusManager;

        private readonly LeftPanelBuilder _leftPanel;
        private readonly RightPanelBuilder _rightPanel;
        private readonly TitleBarBuilder _titleBarBuilder;

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        private string _currentServer = "曙光服 (Infinity)";
        private string _currentFolder;

        private List<UserFileItem> _userFileItems = new List<UserFileItem>();
        private List<CharacterFileItem> _charFileItems = new List<CharacterFileItem>();
        private List<BackupItem> _backupItems = new List<BackupItem>();

        private HelpForm _helpForm;
        private LogForm _logForm;

        private readonly Dictionary<string, string> _serverKeywords = new Dictionary<string, string>
        {
            { "曙光服 (Infinity)", "infinity" },
            { "晨曦服 (Serenity)", "serenity" },
            { "国际服 (Tranquility)", "tranquility" }
        };

        private readonly Dictionary<string, string> _serverDataSourceMap = new Dictionary<string, string>
        {
            { "曙光服 (Infinity)", "infinity" },
            { "晨曦服 (Serenity)", "serenity" },
            { "国际服 (Tranquility)", "tq" }
        };

        public string CurrentFolder => _currentFolder;

        public MainForm()
        {
            _configManager = new ConfigManager();
            _currentServer = _configManager.GetLastServer();

            _logService = new LogService();

            var folderFinder = new FolderFinder(_serverKeywords, _logService.Log, null);

            var fileListService = new FileListService(
                _httpClient,
                _serverDataSourceMap,
                _currentServer,
                _logService.Log,
                null
            );

            var fileSyncManager = new FileSyncManager();

            _fileListRefreshService = new FileListRefreshService(
                fileListService,
                _configManager,
                _logService,
                () => _currentFolder,
                action => action.Invoke()
            );

            _folderService = new FolderService(
                _configManager,
                folderFinder,
                _logService,
                async (folder) => await LoadConfigFilesAsync(folder)
            );
            _folderService.SetCurrentServer(_currentServer);

            _backupService = new BackupService(
                fileSyncManager,
                _logService,
                _configManager,
                () => _currentFolder,
                action => action.Invoke(),
                () => RefreshBackupList()
            );

            _syncService = new SyncService(
                fileSyncManager,
                null,
                null,
                _configManager,
                _logService.Log
            );

            _serverStatusManager = new ServerStatusManager(_httpClient, _currentFolder, _logService.Log);

            _dataGridViewHandler = new DataGridViewHandler(
                _backupService,
                _syncService,
                () => _currentFolder,
                () => _userFileItems,
                () => _charFileItems,
                () => _backupItems,
                (item) => ShowUserSyncDialog((UserFileItem)item),
                (item) => ShowCharSyncDialog((CharacterFileItem)item)
            );

            _leftPanel = new LeftPanelBuilder();
            _rightPanel = new RightPanelBuilder();
            _titleBarBuilder = new TitleBarBuilder(this);

            InitializeComponent();

            // 加载并应用当前主题
            if (_configManager.Config.UseDarkMode)
            {
                ThemeManager.SetDarkMode(true);
                _titleBarBuilder.ApplyTheme(true);
            }
            ThemeManager.ThemeChanged += ApplyTheme;

            _serverStatusManager.SetStatusLabels(
                _leftPanel.LblInfinityStatus,
                _leftPanel.LblSerenityStatus,
                _leftPanel.LblTranquilityStatus
            );

            BindEvents();

            _leftPanel.CmbServer.SelectedItem = _currentServer;

            _logService.Log("程序启动", "成功", "");
            _ = AutoFindFolderAsync();
            _serverStatusManager.Start();
            _ = CheckForUpdatesAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "EVE配置管理工具";
            this.Size = new Size(1100, 588);
            this.MinimumSize = new Size(950, 588);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 248, 255);

            this.Controls.Add(_titleBarBuilder.Build());
            _titleBarBuilder.BtnHelp.Click += BtnHelp_Click;
            _titleBarBuilder.BtnLog.Click += BtnLog_Click;
            _titleBarBuilder.BtnTheme.Click += BtnTheme_Click;
            // 设置按钮已移除
            // _titleBarBuilder.BtnSettings.Click += BtnSettings_Click;

            TableLayoutPanel mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(15, 0, 15, 0),
                Location = new Point(0, 35)
            };
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            mainContainer.Controls.Add(_leftPanel.Build(), 0, 0);
            mainContainer.Controls.Add(_rightPanel.Build(), 1, 0);

            this.Controls.Add(mainContainer);

            this.FormClosing += (s, e) => _serverStatusManager?.Stop();
        }

        private void BindEvents()
        {
            _leftPanel.CmbServer.SelectedIndexChanged += async (s, e) =>
            {
                string newServer = _leftPanel.CmbServer.SelectedItem?.ToString() ?? "曙光服 (Infinity)";
                await OnServerChanged(newServer);
            };

            _leftPanel.BtnOpenFolder.Click += (s, e) => _folderService.OpenCurrentFolder();
            _leftPanel.BtnLoadDefault.Click += async (s, e) => await _folderService.LoadDefaultFolderAsync();
            _leftPanel.BtnSelectFolder.Click += async (s, e) => await _folderService.ManualSelectFolderAsync(this);
            _leftPanel.BtnVersionManage.Click += async (s, e) => await OpenVersionManage();
            _leftPanel.BtnBackup.Click += (s, e) => _backupService.PerformBackup();
            _leftPanel.BtnDeleteAllBackups.Click += (s, e) => _backupService.DeleteAllBackups();

            // 快捷覆盖
            _leftPanel.BtnSync.Click += async (s, e) =>
            {
                var result = CustomMessageBox.Show(
                    "确定要执行快捷覆盖吗？\n\n" +
                    "将用当前最新的配置文件覆盖所有其他配置文件。\n" +
                    "建议先点击「备份当前配置」进行备份。",
                    "确认覆盖",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    await _syncService.SyncAllFilesAsync(
                        () => _currentFolder,
                        (folder) => { _currentFolder = folder; },
                        async () => await RefreshFileListAsync(),
                        (msg) => _logService.Log(msg)
                    );
                }
            };

            // ★★★ 绑定用户备注编辑事件 ★★★
            _rightPanel.UserRemarkEdited += OnUserRemarkEdited;

            _rightPanel.DgvUserFiles.CellClick += (s, e) => _dataGridViewHandler.OnUserFileCellClick(e);
            _rightPanel.DgvCharFiles.CellClick += (s, e) => _dataGridViewHandler.OnCharFileCellClick(e);
            _rightPanel.DgvBackups.CellClick += (s, e) => _dataGridViewHandler.OnBackupCellClick(e);
        }

        // ===== 用户备注编辑事件处理 =====
        private void OnUserRemarkEdited(object sender, UserRemarkEditEventArgs e)
        {
            if (string.IsNullOrEmpty(e.UserId)) return;

            // 保存备注
            _configManager.SaveUserRemark(e.UserId, e.Remark);

            // 更新显示
            string displayName = _configManager.GetUserDisplayName(e.UserId);
            _rightPanel.UpdateUserRemarkDisplay(e.UserId, displayName);

            // 更新本地列表中的DisplayName
            foreach (var item in _userFileItems)
            {
                if (item.UserId == e.UserId)
                {
                    item.DisplayName = displayName;
                    break;
                }
            }

            _logService.Log("用户备注", string.IsNullOrEmpty(e.Remark) ? "删除" : "更新", $"{e.UserId} → {e.Remark}");
        }

        private async Task OnServerChanged(string newServer)
        {
            _currentServer = newServer;
            _configManager.SaveLastServer(newServer);

            await _folderService.SwitchServerAsync(
                newServer,
                (server) => ShowSearchFailDialog()
            );

            _logService.Log("切换服务器", "完成", newServer);
        }

        private async Task AutoFindFolderAsync()
        {
            _leftPanel.BtnOpenFolder.Text = "正在查找...";
            _leftPanel.BtnOpenFolder.Enabled = false;

            await _folderService.AutoFindFolderAsync(() => { });

            UpdateFolderButtonState();
        }

        private async Task LoadConfigFilesAsync(string folder)
        {
            _currentFolder = folder;
            UpdateFolderButtonState();

            await RefreshFileListAsync();
            RefreshBackupList();

            _configManager.Save();
            _logService.Log("加载配置文件", "成功", folder);
        }

        private void UpdateFolderButtonState()
        {
            _folderService.UpdateFolderButtonState((text, enabled) =>
            {
                _leftPanel.BtnOpenFolder.Text = text;
                _leftPanel.BtnOpenFolder.Enabled = enabled;
            });
        }

        private async Task RefreshFileListAsync()
        {
            await _fileListRefreshService.RefreshFileListAsync(
                (grid, items) =>
                {
                    _userFileItems = items;
                    _rightPanel.DgvUserFiles.Rows.Clear();

                    // ★★★ 获取用户备注 ★★★
                    var remarks = _configManager.GetUserRemarks();

                    foreach (var item in items)
                    {
                        // 获取显示名（有备注显示备注，无备注显示ID）
                        string displayName = _configManager.GetUserDisplayName(item.UserId);
                        item.DisplayName = displayName;

                        int rowIndex = _rightPanel.DgvUserFiles.Rows.Add(
                            displayName,  // ← 显示备注或ID
                            item.ModifyTime.ToString("MM-dd HH:mm"),
                            "💾",
                            "📂"
                        );

                        // ★★★ 存储用户ID到行Tag ★★★
                        if (rowIndex >= 0)
                        {
                            _rightPanel.DgvUserFiles.Rows[rowIndex].Tag = item.UserId;
                        }
                    }
                },
                (grid, items) =>
                {
                    _charFileItems = items;
                    _rightPanel.DgvCharFiles.Rows.Clear();
                    foreach (var item in items)
                    {
                        _rightPanel.DgvCharFiles.Rows.Add(
                            item.CharacterName ?? item.CharacterId,
                            item.CharacterId,
                            item.ModifyTime.ToString("MM-dd HH:mm"),
                            "💾",
                            "📂"
                        );
                    }
                },
                (userCount, charCount, backupCount) =>
                {
                    _rightPanel.LblUserTitle.Text = $"用户配置文件 ({userCount}个文件)";
                    _rightPanel.LblCharTitle.Text = $"角色配置文件 ({charCount}个文件)";
                    _rightPanel.LblBackupTitle.Text = $"备份管理 ({backupCount}个备份)";
                }
            );
        }

        private void RefreshBackupList()
        {
            _fileListRefreshService.RefreshBackupList(
                (grid, items) =>
                {
                    _backupItems = items;
                    _rightPanel.DgvBackups.Rows.Clear();
                    foreach (var item in items)
                    {
                        _rightPanel.DgvBackups.Rows.Add(
                            item.DisplayName,
                            item.CreatedAt.ToString("MM-dd HH:mm"),
                            "📂",
                            "↩️",
                            "🗑️"
                        );
                    }
                },
                new FileSyncManager()
            );
        }

        private void ShowSearchFailDialog()
        {
            using (var dialog = new SearchFailDialog(_currentServer))
            {
                dialog.Owner = this;
                var choice = dialog.ShowDialogAndGetResult();

                switch (choice)
                {
                    case SearchFailDialog.UserChoice.SwitchServer:
                        CustomMessageBox.Show("请从左侧下拉框选择其他服务器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    case SearchFailDialog.UserChoice.DeepSearch:
                        _ = _folderService.DeepSearchAndLoadAsync(this);
                        break;
                    case SearchFailDialog.UserChoice.ManualSelect:
                        _ = _folderService.ManualSelectFolderAsync(this);
                        break;
                    case SearchFailDialog.UserChoice.Cancel:
                        UpdateFolderButtonState();
                        break;
                }
            }
        }

        private async Task OpenVersionManage()
        {
            if (string.IsNullOrEmpty(_currentFolder) || !System.IO.Directory.Exists(_currentFolder))
            {
                CustomMessageBox.Show("请先选择有效的EVE配置文件夹", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dialog = new VersionManageDialog(_currentFolder, (msg) => _logService.Log(msg)))
            {
                dialog.OnSchemesChanged += async () => await RefreshFileListAsync();
                dialog.ShowDialog(this);
            }

            _logService.Log("打开配置方案管理", "成功", "");
        }

        private void ShowUserSyncDialog(UserFileItem sourceItem)
        {
            var otherFiles = _userFileItems.Where(f => f.FilePath != sourceItem.FilePath).ToList();
            if (otherFiles.Count == 0)
            {
                CustomMessageBox.Show("没有其他用户文件可以同步", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // ★★★ 获取用户备注 ★★★
            var remarks = _configManager.GetUserRemarks();

            // ★★★ 创建FileTargetItem时传入UserId ★★★
            var targets = otherFiles.Select(item => new FileTargetItem(
                item.FilePath,
                _configManager.GetUserDisplayName(item.UserId),  // 显示名
                item.UserId  // 用户ID，用于匹配备注
            )).ToList();

            using (var fileDialog = new SyncDialog(
                sourceItem.DisplayName ?? sourceItem.UserId,
                System.IO.Path.GetFileName(_currentFolder),
                targets,
                remarks))  // ← 传入备注字典
            {
                if (fileDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                foreach (var targetPath in fileDialog.SelectedTargets)
                {
                    try
                    {
                        System.IO.File.Copy(sourceItem.FilePath, targetPath, true);
                        _logService.Log("同步用户文件", "成功", $"{System.IO.Path.GetFileName(sourceItem.FilePath)} → {System.IO.Path.GetFileName(targetPath)}");
                    }
                    catch (Exception ex)
                    {
                        _logService.Log("同步用户文件", "失败", $"{System.IO.Path.GetFileName(targetPath)}: {ex.Message}");
                    }
                }
                _ = RefreshFileListAsync();
                CustomMessageBox.Show($"同步完成，共同步 {fileDialog.SelectedTargets.Count} 个文件", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ShowCharSyncDialog(CharacterFileItem sourceItem)
        {
            var otherFiles = _charFileItems.Where(f => f.FilePath != sourceItem.FilePath).ToList();
            if (otherFiles.Count == 0)
            {
                CustomMessageBox.Show("没有其他角色文件可以同步", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // ★★★ 角色文件不需要备注，传入null ★★★
            var targets = otherFiles.Select(item => new FileTargetItem(
                item.FilePath,
                item.CharacterName ?? item.CharacterId
            // 角色文件没有UserId，不需要备注
            )).ToList();

            using (var fileDialog = new SyncDialog(
                sourceItem.CharacterName ?? sourceItem.CharacterId,
                System.IO.Path.GetFileName(_currentFolder),
                targets,
                null))  // ← 角色文件不需要备注
            {
                if (fileDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                foreach (var targetPath in fileDialog.SelectedTargets)
                {
                    try
                    {
                        System.IO.File.Copy(sourceItem.FilePath, targetPath, true);
                        _logService.Log("同步角色文件", "成功", $"{System.IO.Path.GetFileName(sourceItem.FilePath)} → {System.IO.Path.GetFileName(targetPath)}");
                    }
                    catch (Exception ex)
                    {
                        _logService.Log("同步角色文件", "失败", $"{System.IO.Path.GetFileName(targetPath)}: {ex.Message}");
                    }
                }
                _ = RefreshFileListAsync();
                CustomMessageBox.Show($"同步完成，共同步 {fileDialog.SelectedTargets.Count} 个文件", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnHelp_Click(object sender, EventArgs e)
        {
            if (_helpForm == null || _helpForm.IsDisposed)
            {
                _helpForm = new HelpForm();
                _helpForm.Owner = this;
                _helpForm.FormClosed += (s, args) => _helpForm = null;
                _helpForm.Show();
                _logService.Log("打开使用说明", "成功", "");
            }
            else
            {
                _helpForm.Close();
                _helpForm = null;
                _logService.Log("关闭使用说明窗口", "成功", "");
            }
        }

        private void BtnLog_Click(object sender, EventArgs e)
        {
            if (_logForm == null || _logForm.IsDisposed)
            {
                _logForm = new LogForm(_logService.GetLogs().ToList());
                _logForm.Owner = this;
                _logForm.FormClosed += (s, args) => _logForm = null;
                _logForm.Show();
                _logService.Log("查看操作日志", "成功", "");
            }
            else
            {
                _logForm.Close();
                _logForm = null;
                _logService.Log("关闭操作日志窗口", "成功", "");
            }
        }

        private void BtnTheme_Click(object sender, EventArgs e)
        {
            ThemeManager.Toggle();
            _configManager.Config.UseDarkMode = ThemeManager.IsDarkMode;
            _configManager.Save();
            _logService.Log("主题切换", "成功", ThemeManager.IsDarkMode ? "暗色模式" : "亮色模式");
        }

        private void ApplyTheme(bool isDark)
        {
            this.BackColor = ThemeManager.Bg;
            _titleBarBuilder.ApplyTheme(isDark);
            ApplyThemeToControl(this, isDark);
        }

        private static void ApplyThemeToControl(Control parent, bool isDark)
        {
            foreach (Control ctrl in parent.Controls)
            {
                // 跳过标题栏（由 TitleBarBuilder 自管理）
                if (ctrl is Panel panel && panel.Dock == DockStyle.Top && panel.Height <= 40)
                    continue;

                if (ctrl is DataGridView dgv)
                {
                    dgv.BackgroundColor = ThemeManager.GridBg;
                    dgv.DefaultCellStyle.BackColor = ThemeManager.GridBg;
                    dgv.DefaultCellStyle.ForeColor = ThemeManager.Text;
                    dgv.DefaultCellStyle.SelectionBackColor = ThemeManager.SelectionBg;
                    dgv.DefaultCellStyle.SelectionForeColor = ThemeManager.SelectionFg;
                    dgv.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.GridHeader;
                    dgv.ColumnHeadersDefaultCellStyle.ForeColor = ThemeManager.Text;
                    dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeManager.GridHeader;
                    dgv.EnableHeadersVisualStyles = false;
                    dgv.GridColor = ThemeManager.Separator;

                    // 列级样式覆盖：消除硬编码的 BackColor（如用户ID列的白色底色）
                    foreach (DataGridViewColumn col in dgv.Columns)
                    {
                        col.DefaultCellStyle.BackColor = ThemeManager.GridBg;
                        col.DefaultCellStyle.ForeColor = ThemeManager.Text;
                        col.DefaultCellStyle.SelectionBackColor = ThemeManager.SelectionBg;
                        col.DefaultCellStyle.SelectionForeColor = ThemeManager.SelectionFg;
                    }
                }
                else if (ctrl is Label label)
                {
                    if (!label.ForeColor.IsEmpty)
                        label.ForeColor = ThemeManager.Text;
                }
                else if (ctrl is Panel p)
                {
                    // 高度≤2且宽度远大于高度 → 分割线，用 Separator 色
                    if (p.Height <= 2 && p.Width > p.Height * 10)
                        p.BackColor = ThemeManager.Separator;
                    else
                        p.BackColor = ThemeManager.Panel;
                }

                if (ctrl.HasChildren)
                    ApplyThemeToControl(ctrl, isDark);
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                string json = await _httpClient.GetStringAsync(AppInfo.UpdateCheckUrl);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string remoteVersion = root.GetProperty("version").GetString();
                string downloadUrl = root.TryGetProperty("url", out var u) ? u.GetString() : AppInfo.ReleasesUrl;
                string notes = root.TryGetProperty("notes", out var n) ? n.GetString() : "";

                if (IsNewerVersion(remoteVersion, AppInfo.Version))
                {
                    this.Invoke(new Action(() =>
                    {
                        using var dialog = new UpdateDialog(remoteVersion, notes, downloadUrl);
                        dialog.Owner = this;
                        dialog.ShowDialog();
                    }));
                    _logService.Log("版本检查", "发现新版本", remoteVersion);
                }
                else
                {
                    _logService.Log("版本检查", "已是最新", AppInfo.Version);
                }
            }
            catch (Exception)
            {
                // 静默处理——网络不通或文件不存在时不影响正常使用
            }
        }

        private static bool IsNewerVersion(string remote, string local)
        {
            try
            {
                // 解析 "v5.3" 格式
                int[] Parse(string v) => v.TrimStart('v', 'V')
                    .Split('.')
                    .Select(s => int.TryParse(s, out int n) ? n : 0)
                    .ToArray();

                int[] r = Parse(remote);
                int[] l = Parse(local);

                int len = Math.Max(r.Length, l.Length);
                for (int i = 0; i < len; i++)
                {
                    int rv = i < r.Length ? r[i] : 0;
                    int lv = i < l.Length ? l[i] : 0;
                    if (rv != lv) return rv > lv;
                }
                return false; // 版本相同
            }
            catch
            {
                return false;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F1:
                    BtnHelp_Click(null, null);
                    return true;
                case Keys.F2:
                    BtnLog_Click(null, null);
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }
    }
}