using EVESyncTool.Core;
using EVESyncTool.Core.Config;
using EVESyncTool.Core.Mapping;
using EVESyncTool.Core.Services;
using EVESyncTool.Core.Services.Backup;
using EVESyncTool.Core.Services.File;
using EVESyncTool.Core.Services.Folder;
using EVESyncTool.Core.Services.Grid;
using EVESyncTool.Core.Services.Log;
using EVESyncTool.Core.Services.ServerStatus;
using EVESyncTool.Core.Services.Sync;
using EVESyncTool.Core.Services.Update;
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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
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
        private readonly FileListService _fileListService;
        private readonly BackupService _backupService;
        private readonly SyncService _syncService;
        private readonly DataGridViewHandler _dataGridViewHandler;
        private readonly ServerStatusManager _serverStatusManager;
        private readonly UpdateDownloader _updateDownloader;

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

        // 运行期间持续检查更新（每 20 分钟一次，同一版本只提醒一次）
        private readonly System.Windows.Forms.Timer _updateCheckTimer;
        private string _lastNotifiedVersion;

        public string CurrentFolder => _currentFolder;

        public MainForm()
        {
            _configManager = new ConfigManager();
            _currentServer = _configManager.GetLastServer();

            _logService = new LogService();

            var folderFinder = new FolderFinder(ServerInfo.ToKeywordMap(), _logService.Log, null);

            _updateDownloader = new UpdateDownloader(_httpClient);

            _fileListService = new FileListService(
                _httpClient,
                _currentServer,
                _logService.Log,
                null
            );

            var fileSyncManager = new FileSyncManager();

            _fileListRefreshService = new FileListRefreshService(
                _fileListService,
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

            // 加载并应用上次的主题模式（保存于 evesync_config.json 的 UseDarkMode）
            ThemeManager.SetDarkMode(_configManager.Config.UseDarkMode);
            ThemeManager.ThemeChanged += ApplyTheme;
            ApplyTheme(ThemeManager.IsDarkMode);

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

            // 启动时立即检查一次，之后每分钟再查（网络不稳定时尽快捕获到新版本）
            _updateCheckTimer = new System.Windows.Forms.Timer();
            _updateCheckTimer.Interval = 60 * 1000;
            _updateCheckTimer.Tick += async (s, e) => await CheckForUpdatesAsync();
            _updateCheckTimer.Start();
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
            _titleBarBuilder.BtnCheckUpdate.Click += BtnCheckUpdate_Click;
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

            this.FormClosing += (s, e) =>
            {
                _serverStatusManager?.Stop();
                _updateCheckTimer?.Stop();
            };
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

            // ★ 同步角色名查询的服务器，否则会用旧服务器查询新服务器的角色 → 一直"查询中"
            _fileListService.UpdateServer(newServer);

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

            // 创建目标列表：显示名（备注优先）+ 用户ID（用于匹配备注）
            var targets = otherFiles.Select(item => new FileTargetItem(
                item.FilePath,
                _configManager.GetUserDisplayName(item.UserId),
                item.UserId
            )).ToList();

            ShowSyncDialog(
                sourceItem.FilePath,
                sourceItem.DisplayName ?? sourceItem.UserId,
                targets,
                _configManager.GetUserRemarks(),
                "同步用户文件");
        }

        private void ShowCharSyncDialog(CharacterFileItem sourceItem)
        {
            var otherFiles = _charFileItems.Where(f => f.FilePath != sourceItem.FilePath).ToList();
            if (otherFiles.Count == 0)
            {
                CustomMessageBox.Show("没有其他角色文件可以同步", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 角色文件没有备注，DisplayName 为角色名或ID
            var targets = otherFiles.Select(item => new FileTargetItem(
                item.FilePath,
                item.CharacterName ?? item.CharacterId
            )).ToList();

            ShowSyncDialog(
                sourceItem.FilePath,
                sourceItem.CharacterName ?? sourceItem.CharacterId,
                targets,
                null,
                "同步角色文件");
        }

        /// <summary>
        /// 通用同步对话框：选择目标 → 复制 → 刷新列表
        /// </summary>
        private void ShowSyncDialog(
            string sourceFilePath,
            string sourceDisplayName,
            List<FileTargetItem> targets,
            Dictionary<string, string> remarks,
            string operationName)
        {
            using (var fileDialog = new SyncDialog(
                sourceDisplayName,
                System.IO.Path.GetFileName(_currentFolder),
                targets,
                remarks))
            {
                if (fileDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                int count = _syncService.CopyFileToTargets(sourceFilePath, fileDialog.SelectedTargets, operationName);
                _ = RefreshFileListAsync();
                CustomMessageBox.Show($"同步完成，共同步 {count} 个文件", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            _logService.Log("主题切换", "成功", ThemeManager.IsDarkMode ? "夜间模式" : "日间模式");
        }

        private async void BtnCheckUpdate_Click(object sender, EventArgs e)
        {
            _logService.Log("版本检查", "手动触发", "");
            await CheckForUpdatesAsync(showResultWhenUpToDate: true);
        }

        private void ApplyTheme(bool isDark)
        {
            _titleBarBuilder.ApplyTheme(isDark);
            ThemeManager.ApplyToForm(this);
        }

        private async Task CheckForUpdatesAsync(bool showResultWhenUpToDate = false)
        {
            string lastError = null;

            // 多地址轮询：依次尝试，哪个能访问用哪个
            foreach (string url in AppInfo.UpdateCheckUrls)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    string json = await _httpClient.GetStringAsync(url, cts.Token);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    string remoteVersion = root.GetProperty("version").GetString();
                    string downloadUrl = root.TryGetProperty("downloadUrl", out var d) ? d.GetString()
                        : root.TryGetProperty("url", out var u) ? u.GetString() : AppInfo.ReleasesUrl;
                    string notes = root.TryGetProperty("notes", out var n) ? n.GetString() : "";

                    if (IsNewerVersion(remoteVersion, AppInfo.Version))
                    {
                        // 同一版本本次运行只提醒一次（点"稍后提醒"后不再重复弹）
                        if (remoteVersion == _lastNotifiedVersion)
                            return;
                        _lastNotifiedVersion = remoteVersion;

                        this.Invoke(new Action(async () =>
                        {
                            using var dialog = new UpdateDialog(remoteVersion, notes, downloadUrl);
                            dialog.Owner = this;
                            if (dialog.ShowDialog() == DialogResult.OK)
                            {
                                await DownloadAndInstallUpdateAsync(remoteVersion, downloadUrl);
                            }
                        }));
                        _logService.Log("版本检查", "发现新版本", remoteVersion);
                    }
                    else
                    {
                        _logService.Log("版本检查", "已是最新", AppInfo.Version);
                        if (showResultWhenUpToDate)
                        {
                            this.Invoke(new Action(() =>
                                CustomMessageBox.Show($"当前已是最新版本 {AppInfo.Version}", "版本检查",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information)));
                        }
                    }
                    return; // 任一地址成功即结束
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    // 尝试下一个地址
                }
            }

            // 所有地址都失败
            _logService.Log("版本检查", "失败", lastError ?? "无法连接更新服务器");
            if (showResultWhenUpToDate)
            {
                this.Invoke(new Action(() =>
                    CustomMessageBox.Show($"检查更新失败，请检查网络连接。\n\n{lastError}", "版本检查",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning)));
            }
        }

        /// <summary>
        /// 下载新版本并自动替换重启
        /// </summary>
        private async Task DownloadAndInstallUpdateAsync(string version, string downloadUrl)
        {
            try
            {
                string exePath = Application.ExecutablePath;
                string dir = Path.GetDirectoryName(exePath) ?? string.Empty;
                string newExePath = Path.Combine(dir, Path.GetFileNameWithoutExtension(exePath) + ".new.exe");

                using var dialog = new DownloadProgressDialog(version);
                dialog.Owner = this;
                dialog.Show();

                var progress = new Progress<int>(p => dialog.UpdateProgress(p, $"已下载 {p}%"));
                bool ok = await Task.Run(() =>
                    _updateDownloader.DownloadAndPrepareAsync(downloadUrl, newExePath, progress, CancellationToken.None));

                if (dialog.IsCancelled)
                {
                    dialog.Close();
                    _logService.Log("自动更新", "已取消", version);
                    return;
                }

                if (!ok)
                {
                    dialog.Close();
                    CustomMessageBox.Show("下载失败，请稍后重试，或点击标题栏 GitHub/Gitee 按钮手动下载。",
                        "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _logService.Log("自动更新", "下载失败", downloadUrl);
                    return;
                }

                dialog.UpdateProgress(100, "下载完成，即将重启安装...");
                await Task.Delay(600);

                // 启动替换脚本：杀进程 → 替换 exe → 重启 → 自删
                _updateDownloader.ApplyUpdateAndRestart(exePath, newExePath);
                dialog.Close();
                _logService.Log("自动更新", "成功", $"已下载 {version}，程序即将重启");
                Application.Exit();
            }
            catch (Exception ex)
            {
                _logService.Log("自动更新", "异常", ex.Message);
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