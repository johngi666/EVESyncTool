using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EVESyncTool.Dialogs.Progress
{
    public partial class SearchProgressForm : Form
    {
        private ProgressBar progressBar;
        private Label lblStatus;
        private bool _isCancelled = false;

        public bool IsCancelled => _isCancelled;

        public SearchProgressForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "深度搜索";
            this.Size = new Size(400, 150);
            this.StartPosition = FormStartPosition.CenterParent;  // 保持 CenterParent
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

            // 拖动功能
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
                Text = "深度搜索",
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
            btnClose.Click += (s, e) => { _isCancelled = true; this.Close(); };

            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(btnClose);

            // 状态标签
            lblStatus = new Label
            {
                Text = "正在搜索设置文件夹...",
                Font = new Font("Microsoft YaHei", 10),
                ForeColor = Color.FromArgb(51, 51, 51),
                AutoSize = false,
                Size = new Size(360, 25),
                Location = new Point(20, 55),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 进度条
            progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                Location = new Point(20, 85),
                Size = new Size(360, 20)
            };

            // 取消按钮
            Button btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(80, 30),
                Location = new Point(310, 115),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { _isCancelled = true; this.Close(); };

            this.Controls.Add(titleBar);
            this.Controls.Add(lblStatus);
            this.Controls.Add(progressBar);
            this.Controls.Add(btnCancel);

            this.Resize += (s, e) =>
            {
                btnClose.Location = new Point(this.Width - 40, 0);
                btnCancel.Location = new Point(this.Width - 90, this.Height - 45);
                progressBar.Size = new Size(this.Width - 40, 20);
                lblStatus.Size = new Size(this.Width - 40, 25);
            };
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // 确保在 Owner 设置后重新计算位置
            if (Owner != null)
            {
                this.Location = new Point(
                    Owner.Location.X + (Owner.Width - this.Width) / 2,
                    Owner.Location.Y + (Owner.Height - this.Height) / 2
                );
            }
        }

        public void UpdateStatus(string status)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => lblStatus.Text = status));
            }
            else
            {
                lblStatus.Text = status;
            }
            Application.DoEvents();
        }

        public void CloseForm()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => this.Close()));
            }
            else
            {
                this.Close();
            }
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    }
}