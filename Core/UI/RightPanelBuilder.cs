using System;
using System.Drawing;
using System.Windows.Forms;

namespace EVESyncTool.Core.UI
{
    /// <summary>
    /// 右侧面板构建器（用户文件、备份管理、角色文件）
    /// </summary>
    public class RightPanelBuilder
    {
        private readonly Panel _panel;

        // 用户文件列表
        private DataGridView _dgvUserFiles;
        private Label _lblUserTitle;

        // 角色文件列表
        private DataGridView _dgvCharFiles;
        private Label _lblCharTitle;

        // 备份管理列表
        private DataGridView _dgvBackups;
        private Label _lblBackupTitle;

        // 列头（供外部访问）
        public ColumnHeader ColUserId { get; private set; }
        public ColumnHeader ColUserTime { get; private set; }
        public ColumnHeader ColUserBackup { get; private set; }
        public ColumnHeader ColUserSync { get; private set; }

        public ColumnHeader ColCharName { get; private set; }
        public ColumnHeader ColCharId { get; private set; }
        public ColumnHeader ColCharTime { get; private set; }
        public ColumnHeader ColCharBackup { get; private set; }
        public ColumnHeader ColCharSync { get; private set; }

        public ColumnHeader ColBackupName { get; private set; }
        public ColumnHeader ColBackupTime { get; private set; }
        public ColumnHeader ColBackupShow { get; private set; }
        public ColumnHeader ColBackupRestore { get; private set; }
        public ColumnHeader ColBackupDelete { get; private set; }

        public DataGridView DgvUserFiles => _dgvUserFiles;
        public DataGridView DgvCharFiles => _dgvCharFiles;
        public DataGridView DgvBackups => _dgvBackups;
        public Label LblUserTitle => _lblUserTitle;
        public Label LblCharTitle => _lblCharTitle;
        public Label LblBackupTitle => _lblBackupTitle;

        public RightPanelBuilder()
        {
            _panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 10, 10, 10),
                BackColor = Color.White
            };

            TableLayoutPanel rightContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            rightContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 370));
            rightContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 640));

            // ===== 左侧：用户 + 备份 =====
            Panel leftColumn = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 10, 0),
                BackColor = Color.White
            };

            TableLayoutPanel leftColumnContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            leftColumnContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 300));
            leftColumnContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 300));

            Panel userPanel = CreateUserFilePanel();
            leftColumnContainer.Controls.Add(userPanel, 0, 0);

            Panel backupPanel = CreateBackupPanel();
            leftColumnContainer.Controls.Add(backupPanel, 0, 1);

            leftColumn.Controls.Add(leftColumnContainer);

            // ===== 右侧：角色文件 =====
            Panel charPanel = CreateCharFilePanel();

            rightContainer.Controls.Add(leftColumn, 0, 0);
            rightContainer.Controls.Add(charPanel, 1, 0);

            _panel.Controls.Add(rightContainer);
        }

        private Panel CreateUserFilePanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 5),
                BackColor = Color.White
            };

            _lblUserTitle = new Label
            {
                Text = "用户配置文件 (0个文件)",
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                AutoSize = true,
                Location = new Point(0, 20)
            };

            _dgvUserFiles = new DataGridView
            {
                Location = new Point(0, 48),
                Size = new Size(panel.Width - 10, 245),
                BorderStyle = BorderStyle.Fixed3D,
                BackgroundColor = Color.FromArgb(248, 248, 248),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                Font = new Font("Microsoft YaHei", 9),
                ScrollBars = ScrollBars.Vertical,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                    BackColor = Color.FromArgb(240, 248, 255),
                    ForeColor = Color.Black,
                    SelectionBackColor = Color.FromArgb(240, 248, 255),
                    SelectionForeColor = Color.Black
                }
            };

            DataGridViewTextBoxColumn colUserId = new DataGridViewTextBoxColumn
            {
                HeaderText = "用户ID",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            DataGridViewTextBoxColumn colUserTime = new DataGridViewTextBoxColumn
            {
                HeaderText = "修改时间",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            DataGridViewButtonColumn colUserBackup = new DataGridViewButtonColumn
            {
                HeaderText = "备份",
                Width = 67,
                Text = "💾",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            DataGridViewButtonColumn colUserSync = new DataGridViewButtonColumn
            {
                HeaderText = "同步",
                Width = 68,
                Text = "📂",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };

            _dgvUserFiles.Columns.Add(colUserId);
            _dgvUserFiles.Columns.Add(colUserTime);
            _dgvUserFiles.Columns.Add(colUserBackup);
            _dgvUserFiles.Columns.Add(colUserSync);

            panel.Controls.Add(_lblUserTitle);
            panel.Controls.Add(_dgvUserFiles);

            return panel;
        }

        private Panel CreateBackupPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 0),
                BackColor = Color.White
            };

            _lblBackupTitle = new Label
            {
                Text = "备份管理 (0个备份)",
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                AutoSize = true,
                Location = new Point(0, 00)
            };

            _dgvBackups = new DataGridView
            {
                Location = new Point(0, 28),
                Size = new Size(panel.Width - 10, 225),
                BorderStyle = BorderStyle.Fixed3D,
                BackgroundColor = Color.FromArgb(248, 248, 248),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                Font = new Font("Microsoft YaHei", 9),
                ScrollBars = ScrollBars.Vertical,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                    BackColor = Color.FromArgb(240, 248, 255),
                    ForeColor = Color.Black,
                    SelectionBackColor = Color.FromArgb(240, 248, 255),
                    SelectionForeColor = Color.Black
                }
            };

            DataGridViewTextBoxColumn colBackupName = new DataGridViewTextBoxColumn
            {
                HeaderText = "备份名",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            DataGridViewTextBoxColumn colBackupTime = new DataGridViewTextBoxColumn
            {
                HeaderText = "时间",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            DataGridViewButtonColumn colBackupShow = new DataGridViewButtonColumn
            {
                HeaderText = "显示",
                Width = 45,
                Text = "📂",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            DataGridViewButtonColumn colBackupRestore = new DataGridViewButtonColumn
            {
                HeaderText = "还原",
                Width = 45,
                Text = "↩️",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            DataGridViewButtonColumn colBackupDelete = new DataGridViewButtonColumn
            {
                HeaderText = "删除",
                Width = 45,
                Text = "🗑️",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };

            _dgvBackups.Columns.Add(colBackupName);
            _dgvBackups.Columns.Add(colBackupTime);
            _dgvBackups.Columns.Add(colBackupShow);
            _dgvBackups.Columns.Add(colBackupRestore);
            _dgvBackups.Columns.Add(colBackupDelete);

            panel.Controls.Add(_lblBackupTitle);
            panel.Controls.Add(_dgvBackups);

            return panel;
        }

        private Panel CreateCharFilePanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.White
            };

            _lblCharTitle = new Label
            {
                Text = "角色配置文件 (0个文件)",
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                AutoSize = true,
                Location = new Point(0, 24)
            };

            _dgvCharFiles = new DataGridView
            {
                Location = new Point(0, 50),
                Size = new Size(panel.Width - 200, 506),
                BorderStyle = BorderStyle.Fixed3D,
                BackgroundColor = Color.FromArgb(248, 248, 248),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                Font = new Font("Microsoft YaHei", 9),
                ScrollBars = ScrollBars.Vertical,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                    BackColor = Color.FromArgb(240, 248, 255),
                    ForeColor = Color.Black,
                    SelectionBackColor = Color.FromArgb(240, 248, 255),
                    SelectionForeColor = Color.Black
                }
            };

            DataGridViewTextBoxColumn colCharName = new DataGridViewTextBoxColumn
            {
                HeaderText = "角色名",
                Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            DataGridViewTextBoxColumn colCharId = new DataGridViewTextBoxColumn
            {
                HeaderText = "角色ID",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            DataGridViewTextBoxColumn colCharTime = new DataGridViewTextBoxColumn
            {
                HeaderText = "修改时间",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            DataGridViewButtonColumn colCharBackup = new DataGridViewButtonColumn
            {
                HeaderText = "备份",
                Width = 50,
                Text = "💾",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };
            DataGridViewButtonColumn colCharSync = new DataGridViewButtonColumn
            {
                HeaderText = "同步",
                Width = 50,
                Text = "📂",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat
            };

            _dgvCharFiles.Columns.Add(colCharName);
            _dgvCharFiles.Columns.Add(colCharId);
            _dgvCharFiles.Columns.Add(colCharTime);
            _dgvCharFiles.Columns.Add(colCharBackup);
            _dgvCharFiles.Columns.Add(colCharSync);

            panel.Controls.Add(_lblCharTitle);
            panel.Controls.Add(_dgvCharFiles);

            return panel;
        }

        public Panel Build()
        {
            return _panel;
        }
    }
}