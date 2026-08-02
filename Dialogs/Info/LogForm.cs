using EVESyncTool.Core.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EVESyncTool.Dialogs.Info
{
    public partial class LogForm : Form
    {
        private List<string> _logEntries;
        private RichTextBox rtb;
        private int _lastLogCount = 0;

        public LogForm(List<string> logEntries)
        {
            _logEntries = logEntries;
            InitializeComponent();
            RefreshLogContent();
            ThemeManager.ApplyToForm(this);
        }

        private void InitializeComponent()
        {
            this.Text = "操作日志";
            this.Size = new Size(490, 588);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = false;
            this.ShowInTaskbar = false;

            // 标题栏
            Panel titleBar = new Panel
            {
                Height = 35,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(70, 130, 180)
            };

            // 添加拖动功能
            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };

            Label titleLabel = new Label
            {
                Text = "操作日志",
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
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
            btnClose.Click += (s, e) => this.Close();

            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(btnClose);

            // 内容区域
            rtb = new RichTextBox
            {
                Location = new Point(10, 50),
                Size = new Size(470, 370),
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(248, 248, 248),
                BorderStyle = BorderStyle.None
            };

            Button btnCloseBottom = new Button
            {
                Text = "关闭",
                Size = new Size(80, 30),
                Location = new Point(400, 420),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCloseBottom.FlatAppearance.BorderSize = 0;
            btnCloseBottom.Click += (s, e) => this.Close();

            this.Controls.Add(titleBar);
            this.Controls.Add(rtb);
            this.Controls.Add(btnCloseBottom);

            // 定时检查是否有新日志
            System.Windows.Forms.Timer refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 500;
            refreshTimer.Tick += (s, e) => AppendNewLogs();
            refreshTimer.Start();

            this.FormClosed += (s, e) => refreshTimer.Stop();

            this.Resize += (s, e) =>
            {
                btnClose.Location = new Point(this.Width - 40, 0);
                rtb.Size = new Size(this.Width - 20, this.Height - 100);
                btnCloseBottom.Location = new Point(this.Width - 90, this.Height - 45);
            };
        }

        private void AppendNewLogs()
        {
            if (rtb == null || rtb.IsDisposed) return;

            int currentCount = _logEntries.Count;
            if (currentCount > _lastLogCount)
            {
                for (int i = _lastLogCount; i < currentCount; i++)
                {
                    if (rtb.Text.Length > 0)
                        rtb.AppendText(Environment.NewLine);
                    rtb.AppendText(_logEntries[i]);
                }

                rtb.SelectionStart = rtb.Text.Length;
                rtb.ScrollToCaret();
                _lastLogCount = currentCount;
            }
        }

        private void RefreshLogContent()
        {
            if (rtb == null || rtb.IsDisposed) return;

            rtb.Text = _logEntries.Count > 0 ? string.Join(Environment.NewLine, _logEntries) : "暂无操作日志";
            _lastLogCount = _logEntries.Count;
            rtb.SelectionStart = rtb.Text.Length;
            rtb.ScrollToCaret();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdatePosition();
            RefreshLogContent();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (Owner != null)
            {
                Owner.LocationChanged += Owner_LocationChanged;
            }
        }

        private void Owner_LocationChanged(object sender, EventArgs e)
        {
            UpdatePosition();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (Owner != null)
            {
                Owner.LocationChanged -= Owner_LocationChanged;
            }
            base.OnFormClosed(e);
        }

        public void UpdatePosition()
        {
            if (Owner != null)
            {
                this.Location = new Point(Owner.Location.X + Owner.Width, Owner.Location.Y);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    }
}