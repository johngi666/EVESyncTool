using EVESyncTool.Core;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EVESyncTool.Core.UI
{
    /// <summary>
    /// 标题栏构建器
    /// </summary>
    public class TitleBarBuilder
    {
        private readonly Form _owner;
        private readonly Panel _titleBar;
        private readonly Button _btnHelp;
        private readonly Button _btnLog;
        private readonly Button _btnCheckUpdate;
        private readonly Button _btnTheme;
        private readonly Button _btnGithub;
        private readonly Button _btnGitee;

        public Button BtnHelp => _btnHelp;
        public Button BtnLog => _btnLog;
        public Button BtnCheckUpdate => _btnCheckUpdate;
        public Button BtnTheme => _btnTheme;
        public Button BtnGithub => _btnGithub;
        public Button BtnGitee => _btnGitee;

        public TitleBarBuilder(Form owner)
        {
            _owner = owner;

            _titleBar = new Panel
            {
                Height = 35,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(70, 130, 180),
                Margin = new Padding(0)
            };

            // 标题（带版本号）
            Label titleLabel = new Label
            {
                Text = $"EVE配置管理工具 {AppInfo.Version}",
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 5)
            };
            titleLabel.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(_owner.Handle, 0xA1, 0x2, 0);
                }
            };

            // 使用说明按钮
            _btnHelp = new Button
            {
                Text = "📖使用说明",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei", 9),
                Size = new Size(80, 25),
                Location = new Point(500, 4),
                Cursor = Cursors.Hand
            };
            _btnHelp.FlatAppearance.BorderSize = 0;

            // 操作日志按钮
            _btnLog = new Button
            {
                Text = "📋操作日志",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei", 9),
                Size = new Size(80, 25),
                Location = new Point(580, 4),
                Cursor = Cursors.Hand
            };
            _btnLog.FlatAppearance.BorderSize = 0;

            // 检查更新按钮
            _btnCheckUpdate = new Button
            {
                Text = "🔄检查更新",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei", 9),
                Size = new Size(85, 25),
                Location = new Point(755, 4),
                Cursor = Cursors.Hand
            };
            _btnCheckUpdate.FlatAppearance.BorderSize = 0;

            // GitHub 按钮
            _btnGithub = new Button
            {
                Text = "🐙GitHub",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei", 9),
                Size = new Size(85, 25),
                Location = new Point(850, 4),
                Cursor = Cursors.Hand
            };
            _btnGithub.FlatAppearance.BorderSize = 0;
            _btnGithub.Click += (s, e) => OpenUrl("https://github.com/johngi666/EVESyncTool");

            // Gitee 按钮
            _btnGitee = new Button
            {
                Text = "🚩Gitee",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei", 9),
                Size = new Size(80, 25),
                Location = new Point(940, 4),
                Cursor = Cursors.Hand
            };
            _btnGitee.FlatAppearance.BorderSize = 0;
            _btnGitee.Click += (s, e) => OpenUrl("https://gitee.com/minisangel/EVESyncTool");

            // 夜间模式切换按钮
            _btnTheme = new Button
            {
                Text = "🌙夜间模式",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei", 9),
                Size = new Size(90, 25),
                Location = new Point(660, 4),
                Cursor = Cursors.Hand
            };
            _btnTheme.FlatAppearance.BorderSize = 0;

            // 关闭按钮
            Button btnClose = new Button
            {
                Text = "×",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Size = new Size(35, 35),
                Location = new Point(_owner.Width - 40, 0),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => _owner.Close();

            // 最小化按钮
            Button btnMinimize = new Button
            {
                Text = "─",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Size = new Size(35, 35),
                Location = new Point(_owner.Width - 80, 0),
                Cursor = Cursors.Hand
            };
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.Click += (s, e) => _owner.WindowState = FormWindowState.Minimized;

            _titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(_owner.Handle, 0xA1, 0x2, 0);
                }
            };

            _titleBar.Controls.Add(titleLabel);
            _titleBar.Controls.Add(_btnHelp);
            _titleBar.Controls.Add(_btnLog);
            _titleBar.Controls.Add(_btnCheckUpdate);
            _titleBar.Controls.Add(_btnTheme);
            _titleBar.Controls.Add(_btnGithub);
            _titleBar.Controls.Add(_btnGitee);
            _titleBar.Controls.Add(btnClose);
            _titleBar.Controls.Add(btnMinimize);

            _owner.Resize += (s, e) =>
            {
                btnClose.Location = new Point(_owner.Width - 40, 0);
                btnMinimize.Location = new Point(_owner.Width - 80, 0);
            };
        }

        public Panel Build()
        {
            return _titleBar;
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                // 打开浏览器失败时忽略
            }
        }

        /// <summary>
        /// 应用明暗主题到标题栏
        /// </summary>
        public void ApplyTheme(bool isDark)
        {
            _titleBar.BackColor = ThemeManager.TitleBar;
            _btnTheme.Text = isDark ? "☀️日间模式" : "🌙夜间模式";

            // 遍历标题栏内所有子控件
            foreach (Control ctrl in _titleBar.Controls)
            {
                if (ctrl is Label label)
                {
                    label.ForeColor = ThemeManager.TitleBtn;
                }
                else if (ctrl is Button btn)
                {
                    btn.ForeColor = ThemeManager.TitleBtn;
                    btn.BackColor = Color.Transparent;
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    }
}