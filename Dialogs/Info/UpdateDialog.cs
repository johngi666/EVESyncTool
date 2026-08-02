using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace EVESyncTool.Dialogs.Info
{
    /// <summary>
    /// 新版本通知弹窗
    /// </summary>
    public class UpdateDialog : Form
    {
        public UpdateDialog(string newVersion, string releaseNotes, string downloadUrl)
        {
            this.Text = "发现新版本";
            this.Size = new Size(420, 260);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblIcon = new Label
            {
                Text = "🎉",
                Font = new Font("Segoe UI", 32),
                Location = new Point(20, 15),
                AutoSize = true
            };

            Label lblTitle = new Label
            {
                Text = $"发现新版本：{newVersion}",
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 130, 180),
                Location = new Point(70, 20),
                AutoSize = true
            };

            Label lblCurrent = new Label
            {
                Text = $"当前版本：{Core.AppInfo.Version}",
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.Gray,
                Location = new Point(70, 48),
                AutoSize = true
            };

            TextBox txtNotes = new TextBox
            {
                Text = string.IsNullOrWhiteSpace(releaseNotes) ? "暂无更新说明" : releaseNotes,
                Font = new Font("Microsoft YaHei", 9),
                Location = new Point(20, 80),
                Size = new Size(370, 80),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Vertical
            };

            Button btnDownload = new Button
            {
                Text = "前往下载",
                Size = new Size(110, 35),
                Location = new Point(150, 175),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDownload.FlatAppearance.BorderSize = 0;
            btnDownload.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true }); }
                catch { }
                this.Close();
            };

            Button btnLater = new Button
            {
                Text = "稍后提醒",
                Size = new Size(110, 35),
                Location = new Point(270, 175),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 180, 180),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 10),
                Cursor = Cursors.Hand
            };
            btnLater.FlatAppearance.BorderSize = 0;
            btnLater.Click += (s, e) => this.Close();

            this.Controls.Add(lblIcon);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblCurrent);
            this.Controls.Add(txtNotes);
            this.Controls.Add(btnDownload);
            this.Controls.Add(btnLater);
        }
    }
}
