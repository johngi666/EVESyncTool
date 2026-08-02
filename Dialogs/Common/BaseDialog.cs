using EVESyncTool.Core.UI;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EVESyncTool.Dialogs.Common
{
    /// <summary>
    /// 弹窗基类：统一无边框样式、标题栏（标题+关闭按钮+拖动）和暗色主题应用
    /// 派生类只需设置 this.Text，其余由基类处理
    /// </summary>
    public class BaseDialog : Form
    {
        protected readonly Panel TitleBar;
        protected readonly Label TitleLabel;
        protected readonly Button CloseButton;

        public BaseDialog()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;

            TitleBar = new Panel
            {
                Height = 35,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(70, 130, 180)
            };

            TitleLabel = new Label
            {
                Text = "",
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 8)
            };

            CloseButton = new Button
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
            CloseButton.FlatAppearance.BorderSize = 0;
            CloseButton.Click += (s, e) => OnCloseClicked();

            TitleBar.MouseDown += TitleBar_MouseDown;
            TitleLabel.MouseDown += TitleBar_MouseDown;

            TitleBar.Controls.Add(TitleLabel);
            TitleBar.Controls.Add(CloseButton);
            this.Controls.Add(TitleBar);

            this.Resize += (s, e) =>
                CloseButton.Location = new Point(this.Width - 40, 0);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            TitleLabel.Text = this.Text;
            ThemeManager.ApplyToForm(this);
        }

        /// <summary>
        /// 点击标题栏关闭按钮时的行为（派生类可覆写）
        /// </summary>
        protected virtual void OnCloseClicked()
        {
            this.Close();
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, 0xA1, 0x2, 0);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    }
}
