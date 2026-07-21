using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EVESyncTool.Dialogs.Config
{
    /// <summary>
    /// Marshal 配置对话框
    /// 用于配置 Marshal 转换相关选项（.dat ↔ .json）
    /// </summary>
    public partial class MarshalConfigDialog : Form
    {
        // 配置选项
        private bool _autoConvertOnSync = true;
        private bool _keepJsonAfterConvert = false;
        private bool _prettyPrintJson = true;
        private string _marshalDllPath = "marshal_ffi.dll";

        // UI 控件
        private CheckBox chkAutoConvert;
        private CheckBox chkKeepJson;
        private CheckBox chkPrettyPrint;
        private TextBox txtDllPath;
        private Button btnBrowseDll;
        private Button btnTest;
        private Button btnOK;
        private Button btnCancel;
        private Label lblStatus;

        public bool AutoConvertOnSync => _autoConvertOnSync;
        public bool KeepJsonAfterConvert => _keepJsonAfterConvert;
        public bool PrettyPrintJson => _prettyPrintJson;
        public string MarshalDllPath => _marshalDllPath;

        public MarshalConfigDialog(
            bool autoConvertOnSync = true,
            bool keepJsonAfterConvert = false,
            bool prettyPrintJson = true,
            string marshalDllPath = "marshal_ffi.dll")
        {
            _autoConvertOnSync = autoConvertOnSync;
            _keepJsonAfterConvert = keepJsonAfterConvert;
            _prettyPrintJson = prettyPrintJson;
            _marshalDllPath = marshalDllPath;

            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Marshal 配置";
            this.Size = new Size(480, 320);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 标题
            Label lblTitle = new Label
            {
                Text = "Marshal 转换配置",
                Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                Location = new Point(20, 15),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "配置 .dat ↔ .json 转换的相关选项",
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.Gray,
                Location = new Point(20, 45),
                AutoSize = true
            };

            // 分割线
            Panel separator = new Panel
            {
                Location = new Point(15, 70),
                Size = new Size(450, 1),
                BackColor = Color.FromArgb(200, 200, 200)
            };

            // ===== 选项区域 =====
            int y = 85;

            // 1. 自动转换
            chkAutoConvert = new CheckBox
            {
                Text = "同步时自动转换 .dat ↔ .json",
                Location = new Point(25, y),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 10),
                Checked = _autoConvertOnSync
            };
            chkAutoConvert.CheckedChanged += (s, e) => _autoConvertOnSync = chkAutoConvert.Checked;

            y += 35;

            // 2. 保留 JSON
            chkKeepJson = new CheckBox
            {
                Text = "转换后保留 .json 文件（不自动删除）",
                Location = new Point(25, y),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 10),
                Checked = _keepJsonAfterConvert
            };
            chkKeepJson.CheckedChanged += (s, e) => _keepJsonAfterConvert = chkKeepJson.Checked;

            y += 35;

            // 3. 美化 JSON
            chkPrettyPrint = new CheckBox
            {
                Text = "美化 JSON 输出（缩进格式）",
                Location = new Point(25, y),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 10),
                Checked = _prettyPrintJson
            };
            chkPrettyPrint.CheckedChanged += (s, e) => _prettyPrintJson = chkPrettyPrint.Checked;

            y += 45;

            // ===== DLL 路径 =====
            Label lblDll = new Label
            {
                Text = "Marshal DLL 路径:",
                Location = new Point(25, y),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold)
            };

            y += 22;

            txtDllPath = new TextBox
            {
                Location = new Point(25, y),
                Size = new Size(340, 25),
                Font = new Font("Microsoft YaHei", 9),
                Text = _marshalDllPath,
                ReadOnly = false
            };
            txtDllPath.TextChanged += (s, e) => _marshalDllPath = txtDllPath.Text;

            btnBrowseDll = new Button
            {
                Text = "浏览...",
                Location = new Point(370, y),
                Size = new Size(80, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9),
                Cursor = Cursors.Hand
            };
            btnBrowseDll.FlatAppearance.BorderSize = 0;
            btnBrowseDll.Click += BtnBrowseDll_Click;

            y += 35;

            // ===== 状态和测试按钮 =====
            lblStatus = new Label
            {
                Text = "状态: 就绪",
                Location = new Point(25, y),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.Gray
            };

            btnTest = new Button
            {
                Text = "测试 DLL",
                Location = new Point(370, y - 3),
                Size = new Size(80, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 165, 0),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9),
                Cursor = Cursors.Hand
            };
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.Click += BtnTest_Click;

            y += 35;

            // ===== 提示信息 =====
            Label lblHint = new Label
            {
                Text = "提示: marshal_ffi.dll 是 Rust 编译的 Marshal 编解码库",
                Location = new Point(25, y),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 8),
                ForeColor = Color.Gray
            };

            // ===== 底部按钮 =====
            Panel bottomPanel = new Panel
            {
                Location = new Point(0, this.Height - 50),
                Size = new Size(this.Width, 50),
                BackColor = Color.FromArgb(248, 248, 248)
            };

            btnOK = new Button
            {
                Text = "确定",
                Size = new Size(80, 30),
                Location = new Point(300, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); };

            btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(80, 30),
                Location = new Point(385, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(150, 150, 150),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            bottomPanel.Controls.Add(btnOK);
            bottomPanel.Controls.Add(btnCancel);

            // 添加控件
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblSubtitle);
            this.Controls.Add(separator);
            this.Controls.Add(chkAutoConvert);
            this.Controls.Add(chkKeepJson);
            this.Controls.Add(chkPrettyPrint);
            this.Controls.Add(lblDll);
            this.Controls.Add(txtDllPath);
            this.Controls.Add(btnBrowseDll);
            this.Controls.Add(lblStatus);
            this.Controls.Add(btnTest);
            this.Controls.Add(lblHint);
            this.Controls.Add(bottomPanel);

            // 窗口大小变化时调整底部面板
            this.Resize += (s, e) =>
            {
                bottomPanel.Width = this.Width;
                bottomPanel.Location = new Point(0, this.Height - 50);
                btnOK.Location = new Point(this.Width - 180, 10);
                btnCancel.Location = new Point(this.Width - 95, 10);
            };
        }

        private void LoadSettings()
        {
            chkAutoConvert.Checked = _autoConvertOnSync;
            chkKeepJson.Checked = _keepJsonAfterConvert;
            chkPrettyPrint.Checked = _prettyPrintJson;
            txtDllPath.Text = _marshalDllPath;
        }

        private void BtnBrowseDll_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            dialog.Title = "选择 marshal_ffi.dll";
            dialog.Filter = "DLL 文件 (*.dll)|*.dll|所有文件 (*.*)|*.*";
            dialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtDllPath.Text = dialog.FileName;
                _marshalDllPath = dialog.FileName;
                UpdateStatus("DLL 路径已更新", Color.Green);
            }
        }

        private void BtnTest_Click(object sender, EventArgs e)
        {
            string dllPath = txtDllPath.Text;

            if (string.IsNullOrEmpty(dllPath))
            {
                UpdateStatus("错误: 请指定 DLL 路径", Color.Red);
                return;
            }

            if (!File.Exists(dllPath))
            {
                UpdateStatus($"错误: 文件不存在: {dllPath}", Color.Red);
                return;
            }

            try
            {
                // 尝试加载 DLL 以验证其有效性
                // 这里使用 LoadLibrary 验证
                IntPtr handle = LoadLibrary(dllPath);
                if (handle == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    UpdateStatus($"DLL 加载失败，错误码: {error}", Color.Red);
                }
                else
                {
                    FreeLibrary(handle);
                    UpdateStatus("DLL 测试成功！", Color.Green);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"测试失败: {ex.Message}", Color.Red);
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            lblStatus.Text = $"状态: {message}";
            lblStatus.ForeColor = color;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// 显示配置对话框并返回用户选择
        /// </summary>
        
    }
}