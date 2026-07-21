using EVESyncTool.Core.Mapping;
using EVESyncTool.Dialogs.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace EVESyncTool.Dialogs.Sync
{
    public partial class SyncDialog : Form
    {
        private readonly string _sourceName;
        private readonly string _targetFolder;
        private readonly List<FileTargetItem> _targets;
        private readonly List<string> _savedSelections;
        private Dictionary<string, CheckBox> _checkBoxes = new Dictionary<string, CheckBox>();
        private Button btnConfirm;
        private Button btnCancel;
        private Button btnSelectAll;
        private Button btnDeselectAll;
        private FlowLayoutPanel categoryPanel;
        private Label lblInfo;
        private Label lblTargetInfo;
        private ToolTip toolTip = new ToolTip();
        private Panel scrollContainer;

        public List<string> SelectedSettings { get; private set; } = new List<string>();
        public List<string> SelectedTargets { get; private set; } = new List<string>();

        public SyncDialog(string sourceName, string targetFolder)
            : this(sourceName, targetFolder, null, null)
        {
        }

        public SyncDialog(string sourceName, string sourcePath, List<FileTargetItem> targets)
            : this(sourceName, Path.GetFileName(Path.GetDirectoryName(sourcePath)), targets, null)
        {
        }

        public SyncDialog(
            string sourceName,
            string targetFolder,
            List<FileTargetItem> targets,
            List<string> savedSelections)
        {
            _sourceName = sourceName;
            _targetFolder = targetFolder;
            _targets = targets ?? new List<FileTargetItem>();
            _savedSelections = savedSelections ?? new List<string>();

            InitializeComponent();

            if (_targets.Count > 0)
            {
                LoadTargets();
            }
            else
            {
                LoadMappings();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "选择同步目标";
            this.Size = new Size(600, 520);
            this.MinimumSize = new Size(550, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.White;

            Panel titleBar = new Panel
            {
                Height = 35,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(70, 130, 180)
            };

            Label titleLabel = new Label
            {
                Text = "选择要同步的目标",
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 8)
            };

            Button btnClose = new Button
            {
                Text = "×",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Size = new Size(35, 35),
                Location = new Point(this.Width - 40, 0),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; this.Close(); };

            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(btnClose);

            lblInfo = new Label
            {
                Text = $"来源: {_sourceName} → 目标: {_targetFolder}",
                Font = new Font("Microsoft YaHei", 10),
                ForeColor = Color.FromArgb(70, 130, 180),
                AutoSize = true,
                Location = new Point(15, 48)
            };

            lblTargetInfo = new Label
            {
                Text = "",
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(15, 72)
            };

            btnSelectAll = new Button
            {
                Text = "全选",
                Size = new Size(70, 28),
                Location = new Point(15, 98),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSelectAll.FlatAppearance.BorderSize = 0;
            btnSelectAll.Click += (s, e) => SetAllCheckBoxes(true);

            btnDeselectAll = new Button
            {
                Text = "全不选",
                Size = new Size(70, 28),
                Location = new Point(90, 98),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDeselectAll.FlatAppearance.BorderSize = 0;
            btnDeselectAll.Click += (s, e) => SetAllCheckBoxes(false);

            // ★★★ 滚动容器 ★★★
            scrollContainer = new Panel
            {
                Location = new Point(15, 135),
                Size = new Size(570, 290),
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 248, 248),
                BorderStyle = BorderStyle.FixedSingle
            };

            // ★★★ FlowLayoutPanel：AutoSize = false，手动控制大小 ★★★
            categoryPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                Width = scrollContainer.Width - 20,
                BackColor = Color.Transparent,
                Padding = new Padding(10),
                MinimumSize = new Size(10, 10)
            };
            scrollContainer.Controls.Add(categoryPanel);

            // ★★★ 当容器内容变化时，更新 panel 高度 ★★★
            scrollContainer.Resize += (s, e) =>
            {
                categoryPanel.Width = scrollContainer.Width - 20;
            };

            btnConfirm = new Button
            {
                Text = "执行同步",
                Size = new Size(120, 35),
                Location = new Point(this.Width - 195, 438),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 205, 50),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;

            btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(100, 35),
                Location = new Point(this.Width - 100, 438),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; this.Close(); };

            Label lblSummary = new Label
            {
                Text = "提示: 只勾选需要同步的目标，未勾选的将保持不变",
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(15, 445)
            };

            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            contentPanel.Controls.Add(titleBar);
            contentPanel.Controls.Add(lblInfo);
            contentPanel.Controls.Add(lblTargetInfo);
            contentPanel.Controls.Add(btnSelectAll);
            contentPanel.Controls.Add(btnDeselectAll);
            contentPanel.Controls.Add(scrollContainer);
            contentPanel.Controls.Add(btnConfirm);
            contentPanel.Controls.Add(btnCancel);
            contentPanel.Controls.Add(lblSummary);

            this.Controls.Add(contentPanel);

            this.Resize += (s, e) =>
            {
                btnClose.Location = new Point(this.Width - 40, 0);
                scrollContainer.Size = new Size(this.Width - 30, this.Height - 205);
                btnConfirm.Location = new Point(this.Width - 195, this.Height - 62);
                btnCancel.Location = new Point(this.Width - 100, this.Height - 62);
                lblSummary.Location = new Point(15, this.Height - 55);
                categoryPanel.Width = scrollContainer.Width - 20;
            };
        }

        private void LoadTargets()
        {
            lblTargetInfo.Text = $"共 {_targets.Count} 个目标文件可供选择";

            categoryPanel.Controls.Clear();
            categoryPanel.Height = 10;

            Label titleLabel = new Label
            {
                Text = "── 选择要同步的目标文件 ──",
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 4)
            };
            categoryPanel.Controls.Add(titleLabel);

            int totalHeight = 30; // 标题高度

            foreach (var target in _targets)
            {
                CheckBox cb = new CheckBox
                {
                    Text = target.DisplayName ?? Path.GetFileName(target.FileName),
                    Font = new Font("Microsoft YaHei", 9),
                    AutoSize = true,
                    Checked = true,
                    Margin = new Padding(20, 2, 0, 2),
                    Tag = target.FileName
                };
                categoryPanel.Controls.Add(cb);
                _checkBoxes[target.FileName] = cb;
                totalHeight += 26;
            }

            // ★★★ 更新 panel 高度，让滚动条正确工作 ★★★
            categoryPanel.Height = Math.Max(totalHeight + 20, scrollContainer.Height - 10);
        }

        private void LoadMappings()
        {
            lblTargetInfo.Text = "选择要覆盖的设置项";

            var categories = SettingMapping.GetAllCategories();
            _checkBoxes.Clear();
            categoryPanel.Controls.Clear();
            categoryPanel.Height = 10;

            int totalHeight = 10;

            foreach (var category in categories)
            {
                Label categoryTitle = new Label
                {
                    Text = $"── {category} ──",
                    Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 130, 180),
                    AutoSize = true,
                    Margin = new Padding(0, 8, 0, 4)
                };
                categoryPanel.Controls.Add(categoryTitle);
                totalHeight += 30;

                var mappings = SettingMapping.GetAll().Where(m => m.Category == category).ToList();
                foreach (var mapping in mappings)
                {
                    CheckBox cb = new CheckBox
                    {
                        Text = mapping.DisplayName,
                        Font = new Font("Microsoft YaHei", 9),
                        AutoSize = true,
                        Checked = true,
                        Margin = new Padding(20, 2, 0, 2),
                        Tag = mapping
                    };
                    if (!string.IsNullOrEmpty(mapping.Description))
                    {
                        cb.MouseHover += (s, e) => toolTip.SetToolTip(cb, mapping.Description);
                    }
                    categoryPanel.Controls.Add(cb);
                    _checkBoxes[mapping.DisplayName] = cb;
                    totalHeight += 26;
                }
            }

            // ★★★ 更新 panel 高度，让滚动条正确工作 ★★★
            categoryPanel.Height = Math.Max(totalHeight + 20, scrollContainer.Height - 10);
        }

        private void SetAllCheckBoxes(bool checkedState)
        {
            foreach (var cb in _checkBoxes.Values)
            {
                cb.Checked = checkedState;
            }
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            SelectedSettings.Clear();
            SelectedTargets.Clear();

            foreach (var kvp in _checkBoxes)
            {
                if (kvp.Value.Checked)
                {
                    string key = kvp.Key;
                    if (_targets.Count > 0)
                    {
                        SelectedTargets.Add(key);
                    }
                    else
                    {
                        SelectedSettings.Add(key);
                    }
                }
            }

            if (_targets.Count > 0 && SelectedTargets.Count == 0)
            {
                CustomMessageBox.Show("请至少选择一个要同步的目标文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_targets.Count == 0 && SelectedSettings.Count == 0)
            {
                CustomMessageBox.Show("请至少选择一个要覆盖的设置项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    public class FileTargetItem
    {
        public string FileName { get; set; }
        public string DisplayName { get; set; }

        public FileTargetItem(string fileName, string displayName)
        {
            FileName = fileName;
            DisplayName = displayName;
        }
    }
}