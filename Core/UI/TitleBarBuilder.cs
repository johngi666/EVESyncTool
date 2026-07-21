using System;
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
        private readonly Button _btnSettings;

        public Button BtnHelp => _btnHelp;
        public Button BtnLog => _btnLog;
        public Button BtnSettings => _btnSettings;

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

            // 标题
            Label titleLabel = new Label
            {
                Text = "EVE配置管理工具",
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
                Location = new Point(200, 4),
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
                Location = new Point(280, 4),
                Cursor = Cursors.Hand
            };
            _btnLog.FlatAppearance.BorderSize = 0;

            // 覆盖设置按钮
            _btnSettings = new Button
            {
                Text = "⚙覆盖设置",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei", 9),
                Size = new Size(80, 25),
                Location = new Point(360, 4),
                Cursor = Cursors.Hand
            };
            _btnSettings.FlatAppearance.BorderSize = 0;

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
            _titleBar.Controls.Add(_btnSettings);
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

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    }
}