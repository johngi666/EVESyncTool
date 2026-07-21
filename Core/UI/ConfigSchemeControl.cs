using EVESyncTool.Core.Config;
using EVESyncTool.Data;
using EVESyncTool.Dialogs.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESyncTool.Core.UI
{
    public partial class ConfigSchemeControl : UserControl, INotifyPropertyChanged
    {
        private readonly ConfigSchemeManager _manager;
        private string _parentFolder;
        private ObservableCollection<ConfigScheme> _schemes;
        private const int MaxLogEntries = 100;

        // 控件
        private TableLayoutPanel mainLayout;
        private Panel schemesPanel;
        private FlowLayoutPanel schemesContainer;
        private TableLayoutPanel titlePanel;
        private Label lblTitle;
        private TableLayoutPanel addPanel;
        private Label lblDragHint;
        private Button btnAdd;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action OnSchemeChanged;

        public ObservableCollection<ConfigScheme> Schemes
        {
            get => _schemes;
            set { _schemes = value; OnPropertyChanged(); RefreshSchemesList(); }
        }

        public ConfigSchemeControl()
        {
            _manager = new ConfigSchemeManager();
            _schemes = new ObservableCollection<ConfigScheme>();
            InitializeComponent();
            LoadSchemes();
            UpdateEmptyState();
        }

        private void InitializeComponent()
        {
            mainLayout = new TableLayoutPanel();
            titlePanel = new TableLayoutPanel();
            lblTitle = new Label();
            lblDragHint = new Label();
            schemesPanel = new Panel();
            schemesContainer = new FlowLayoutPanel();
            addPanel = new TableLayoutPanel();
            btnAdd = new Button();
            mainLayout.SuspendLayout();
            titlePanel.SuspendLayout();
            schemesPanel.SuspendLayout();
            addPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainLayout
            // 
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            mainLayout.Controls.Add(titlePanel, 0, 0);
            mainLayout.Controls.Add(schemesPanel, 0, 1);
            mainLayout.Controls.Add(addPanel, 0, 2);
            mainLayout.Location = new Point(0, 0);
            mainLayout.Name = "mainLayout";
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            mainLayout.Size = new Size(200, 100);
            mainLayout.TabIndex = 0;
            // 
            // titlePanel
            // 
            titlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            titlePanel.ColumnStyles.Add(new ColumnStyle());
            titlePanel.Controls.Add(lblTitle, 0, 0);
            titlePanel.Controls.Add(lblDragHint, 1, 0);
            titlePanel.Location = new Point(3, 3);
            titlePanel.Name = "titlePanel";
            titlePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            titlePanel.Size = new Size(194, 24);
            titlePanel.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.Location = new Point(3, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(82, 23);
            lblTitle.TabIndex = 0;
            // 
            // lblDragHint
            // 
            lblDragHint.Location = new Point(91, 0);
            lblDragHint.Name = "lblDragHint";
            lblDragHint.Size = new Size(100, 23);
            lblDragHint.TabIndex = 1;
            // 
            // schemesPanel
            // 
            schemesPanel.Controls.Add(schemesContainer);
            schemesPanel.Location = new Point(3, 33);
            schemesPanel.Name = "schemesPanel";
            schemesPanel.Size = new Size(194, 24);
            schemesPanel.TabIndex = 1;
            // 
            // schemesContainer
            // 
            schemesContainer.Location = new Point(0, 0);
            schemesContainer.Name = "schemesContainer";
            schemesContainer.Size = new Size(200, 100);
            schemesContainer.TabIndex = 0;
            // 
            // addPanel
            // 
            addPanel.ColumnStyles.Add(new ColumnStyle());
            addPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            addPanel.Controls.Add(btnAdd, 0, 0);
            addPanel.Location = new Point(3, 63);
            addPanel.Name = "addPanel";
            addPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            addPanel.Size = new Size(194, 34);
            addPanel.TabIndex = 2;
            // 
            // btnAdd
            // 
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Location = new Point(3, 3);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(75, 23);
            btnAdd.TabIndex = 0;
            btnAdd.Click += BtnAdd_Click;
            // 
            // ConfigSchemeControl
            // 
            AllowDrop = true;
            BackColor = Color.White;
            Controls.Add(mainLayout);
            Name = "ConfigSchemeControl";
            Size = new Size(2243, 1036);
            DragDrop += ConfigSchemeControl_DragDrop;
            DragEnter += ConfigSchemeControl_DragEnter;
            mainLayout.ResumeLayout(false);
            titlePanel.ResumeLayout(false);
            schemesPanel.ResumeLayout(false);
            addPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        private Button CreateButton(string text, int x, int y, int width, int height)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 5, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CreateSmallButton(string text, Color backColor)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(60, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 8, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 0, 2, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        public void SetParentFolder(string folder)
        {
            _parentFolder = folder;
        }

        public void RefreshSchemes()
        {
            LoadSchemes();
            schemesContainer.PerformLayout();
            schemesPanel.PerformLayout();
        }

        private void LoadSchemes()
        {
            _schemes.Clear();
            var schemes = _manager.GetAll();
            foreach (var scheme in schemes)
            {
                _schemes.Add(scheme);
            }
            RefreshSchemesList();
        }

        private void RefreshSchemesList()
        {
            schemesContainer.Controls.Clear();
            foreach (var scheme in _schemes)
            {
                var row = CreateSchemeRow(scheme);
                schemesContainer.Controls.Add(row);
            }
            UpdateEmptyState();
        }

        private Panel CreateSchemeRow(ConfigScheme scheme)
        {
            int containerWidth = schemesContainer.Width > 20 ? schemesContainer.Width - 20 : 350;
            Panel row = new Panel
            {
                Width = containerWidth,
                Height = 40,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblName = new Label
            {
                Text = scheme.Name,
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180)
            };

            FlowLayoutPanel btnPanel = new FlowLayoutPanel
            {
                Location = new Point(containerWidth - 310, 5),
                Size = new Size(300, 32),
                FlowDirection = FlowDirection.LeftToRight
            };

            bool pathExists = Directory.Exists(scheme.FolderPath);

            Button btnSwitch = CreateSmallButton("切换", pathExists ? Color.FromArgb(70, 130, 180) : Color.FromArgb(150, 150, 150));
            btnSwitch.Click += (s, e) => BtnSwitch_Click(scheme);
            btnSwitch.Enabled = pathExists;

            Button btnUpdate = CreateSmallButton("更新", pathExists ? Color.FromArgb(50, 205, 50) : Color.FromArgb(150, 150, 150));
            btnUpdate.Click += (s, e) => BtnUpdate_Click(scheme);
            btnUpdate.Enabled = pathExists;

            Button btnBackup = CreateSmallButton("备份", Color.FromArgb(255, 165, 0));
            btnBackup.Click += (s, e) => BtnBackup_Click(scheme);

            Button btnRestore = CreateSmallButton("还原", Color.FromArgb(70, 130, 180));
            btnRestore.Click += (s, e) => BtnRestore_Click(scheme);

            Button btnRemove = CreateSmallButton("移除", Color.FromArgb(255, 69, 0));
            btnRemove.Click += (s, e) => BtnRemove_Click(scheme);

            btnPanel.Controls.Add(btnSwitch);
            btnPanel.Controls.Add(btnUpdate);
            btnPanel.Controls.Add(btnBackup);
            btnPanel.Controls.Add(btnRestore);
            btnPanel.Controls.Add(btnRemove);

            row.Controls.Add(lblName);
            row.Controls.Add(btnPanel);

            row.Resize += (s, e) =>
            {
                row.Width = schemesContainer.Width > 20 ? schemesContainer.Width - 20 : 350;
                btnPanel.Location = new Point(row.Width - 310, 5);
            };

            return row;
        }

        private void UpdateEmptyState()
        {
            if (schemesContainer.Controls.Count == 0)
            {
                Label emptyLabel = new Label
                {
                    Text = "暂无配置方案，点击 [+ 添加配置方案] 或拖拽文件夹到此区域添加",
                    AutoSize = true,
                    Font = new Font("Microsoft YaHei", 10),
                    ForeColor = Color.Gray,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                schemesContainer.Controls.Add(emptyLabel);
            }
        }

        private void BtnSwitch_Click(ConfigScheme scheme)
        {
            if (string.IsNullOrEmpty(_parentFolder) || !Directory.Exists(_parentFolder))
            {
                CustomMessageBox.Show("错误：父文件夹不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!Directory.Exists(scheme.FolderPath))
            {
                CustomMessageBox.Show($"错误：方案文件夹不存在 [{scheme.Name}]", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = CustomMessageBox.Show(
                $"确定将 [{scheme.Name}] 切换到父文件夹吗？\n\n切换前会自动备份当前配置到桌面。",
                "确认切换", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            Task.Run(() =>
            {
                try
                {
                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string backupPath = Path.Combine(desktop, $"EVE_Config_Backup_{timestamp}");
                    Directory.CreateDirectory(backupPath);
                    CopyDirectoryContents(_parentFolder, backupPath);
                }
                catch
                {
                    // 静默处理备份错误
                }
            }).ContinueWith(_ =>
            {
                try
                {
                    CopyDirectoryContents(scheme.FolderPath, _parentFolder);
                    _manager.UpdateLastUsed(scheme.Id);
                    OnSchemeChanged?.Invoke();
                    CustomMessageBox.Show($"切换完成 [{scheme.Name}]", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"切换失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void BtnUpdate_Click(ConfigScheme scheme)
        {
            if (string.IsNullOrEmpty(_parentFolder) || !Directory.Exists(_parentFolder))
            {
                CustomMessageBox.Show("错误：父文件夹不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!Directory.Exists(scheme.FolderPath))
            {
                CustomMessageBox.Show($"错误：方案文件夹不存在 [{scheme.Name}]", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = CustomMessageBox.Show(
                $"将父文件夹配置更新到 [{scheme.Name}]？",
                "确认更新", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            Task.Run(() =>
            {
                try
                {
                    CopyDirectoryContents(_parentFolder, scheme.FolderPath);
                    _manager.UpdateLastUsed(scheme.Id);
                    OnSchemeChanged?.Invoke();
                    CustomMessageBox.Show($"更新完成 [{scheme.Name}]", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"更新失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private async void BtnBackup_Click(ConfigScheme scheme)
        {
            if (!Directory.Exists(scheme.FolderPath))
            {
                CustomMessageBox.Show($"错误：方案文件夹不存在 [{scheme.Name}]", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string backupPath = await Task.Run(() =>
            {
                try
                {
                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string path = Path.Combine(desktop, $"{scheme.Name}_Backup_{timestamp}");
                    Directory.CreateDirectory(path);
                    CopyDirectoryContents(scheme.FolderPath, path);
                    return path;
                }
                catch (Exception)
                {
                    return null;
                }
            });
            if (backupPath != null)
            {
                CustomMessageBox.Show($"备份完成\n保存路径: {backupPath}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                CustomMessageBox.Show("备份失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRestore_Click(ConfigScheme scheme)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择备份文件夹";
            if (dialog.ShowDialog() != DialogResult.OK) return;

            string backupPath = dialog.SelectedPath;
            var result = CustomMessageBox.Show(
                $"将备份还原到 [{scheme.Name}]？\n这将覆盖目标文件夹中的所有文件。",
                "确认还原", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            Task.Run(() =>
            {
                try
                {
                    CopyDirectoryContents(backupPath, scheme.FolderPath);
                    OnSchemeChanged?.Invoke();
                    CustomMessageBox.Show($"还原完成 [{scheme.Name}]", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"还原失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void BtnRemove_Click(ConfigScheme scheme)
        {
            var result = CustomMessageBox.Show(
                $"移除 [{scheme.Name}]？\n此操作不会删除实际文件夹。",
                "确认移除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            _manager.Remove(scheme.Id);
            _schemes.Remove(scheme);
            OnSchemeChanged?.Invoke();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择配置方案文件夹";
            if (dialog.ShowDialog() != DialogResult.OK) return;
            AddScheme(dialog.SelectedPath);
        }

        private void AddScheme(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;

            if (_schemes.Any(s => s.FolderPath == folderPath))
            {
                CustomMessageBox.Show($"文件夹已存在: {Path.GetFileName(folderPath)}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string schemeName = Path.GetFileName(folderPath);
            int counter = 1;
            while (_schemes.Any(s => s.Name == schemeName))
            {
                schemeName = $"{Path.GetFileName(folderPath)} ({counter++})";
            }

            var scheme = new ConfigScheme
            {
                Name = schemeName,
                FolderPath = folderPath
            };

            if (_manager.Add(scheme))
            {
                _schemes.Add(scheme);
                OnSchemeChanged?.Invoke();
            }
            else
            {
                CustomMessageBox.Show($"添加失败: {schemeName}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigSchemeControl_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void ConfigSchemeControl_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    if (Directory.Exists(file))
                        AddScheme(file);
                }
            }
        }

        private void CopyDirectoryContents(string source, string dest)
        {
            if (!Directory.Exists(dest))
                Directory.CreateDirectory(dest);

            foreach (string file in Directory.GetFiles(source))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(dest, fileName);
                File.Copy(file, destFile, true);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}