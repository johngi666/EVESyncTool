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

        private static readonly HttpClient _httpClient = new HttpClient();
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

            // 覆盖设置：加载时确保默认全选
            var settings = _configManager.GetSyncSettings();
            if (settings.SelectedPartialSettings == null || settings.SelectedPartialSettings.Count == 0)
            {
                settings.SelectedPartialSettings = SettingMapping.GetAll().Select(s => s.DisplayName).ToList();
                _configManager.SaveSyncSettings(settings);
                _logService.Log("覆盖设置", "默认全选", $"共 {settings.SelectedPartialSettings.Count} 类");
            }
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
            _titleBarBuilder.BtnSettings.Click += BtnSettings_Click;

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

            // ★★★ 快捷覆盖 - 添加确认弹窗 ★★★
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

            _rightPanel.DgvUserFiles.CellClick += (s, e) => _dataGridViewHandler.OnUserFileCellClick(e);
            _rightPanel.DgvCharFiles.CellClick += (s, e) => _dataGridViewHandler.OnCharFileCellClick(e);
            _rightPanel.DgvBackups.CellClick += (s, e) => _dataGridViewHandler.OnBackupCellClick(e);
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
                    foreach (var item in items)
                    {
                        _rightPanel.DgvUserFiles.Rows.Add(
                            item.DisplayName ?? item.UserId,
                            item.ModifyTime.ToString("MM-dd HH:mm"),
                            "💾",
                            "📂"
                        );
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

            var settings = _configManager.GetSyncSettings();
            var savedSelections = settings.SelectedPartialSettings ?? new List<string>();
            var targets = otherFiles.Select(item => new FileTargetItem(item.FilePath, item.DisplayName ?? item.UserId)).ToList();

            using (var dialog = new SyncDialog(
                sourceItem.DisplayName ?? sourceItem.UserId,
                System.IO.Path.GetFileName(_currentFolder),
                targets,
                savedSelections))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    settings.SelectedPartialSettings = dialog.SelectedSettings;
                    _configManager.SaveSyncSettings(settings);

                    foreach (var targetPath in dialog.SelectedTargets)
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
                    CustomMessageBox.Show($"同步完成，共同步 {dialog.SelectedTargets.Count} 个文件", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
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

            var settings = _configManager.GetSyncSettings();
            var savedSelections = settings.SelectedPartialSettings ?? new List<string>();
            var targets = otherFiles.Select(item => new FileTargetItem(item.FilePath, item.CharacterName ?? item.CharacterId)).ToList();

            using (var dialog = new SyncDialog(
                sourceItem.CharacterName ?? sourceItem.CharacterId,
                System.IO.Path.GetFileName(_currentFolder),
                targets,
                savedSelections))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    settings.SelectedPartialSettings = dialog.SelectedSettings;
                    _configManager.SaveSyncSettings(settings);

                    foreach (var targetPath in dialog.SelectedTargets)
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
                    CustomMessageBox.Show($"同步完成，共同步 {dialog.SelectedTargets.Count} 个文件", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
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

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            var settings = _configManager.GetSyncSettings();
            var savedSelections = settings.SelectedPartialSettings ?? new List<string>();

            using (var dialog = new SyncDialog("覆盖设置", "全局", null, savedSelections))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    settings.SelectedPartialSettings = dialog.SelectedSettings;
                    _configManager.SaveSyncSettings(settings);
                    CustomMessageBox.Show($"已保存 {dialog.SelectedSettings.Count} 类设置", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _logService.Log("覆盖设置", "已保存", $"共 {dialog.SelectedSettings.Count} 类设置");
                }
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