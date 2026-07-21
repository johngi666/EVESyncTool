using EVESyncTool.Dialogs.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESyncTool.Core.Services
{
    public class ServerStatusManager
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, bool> _previousOnlineStatus = new Dictionary<string, bool>
        {
            { "infinity", false },
            { "serenity", false },
            { "tq", false }
        };

        private bool _isFirstStatusCheck = true;
        private string _currentFolder;
        private readonly Action<string, string, string> _logAction;
        private readonly System.Windows.Forms.Timer _statusTimer;

        private Label _lblInfinityStatus;
        private Label _lblSerenityStatus;
        private Label _lblTranquilityStatus;

        public ServerStatusManager(HttpClient httpClient, string currentFolder, Action<string, string, string> logAction)
        {
            _httpClient = httpClient;
            _currentFolder = currentFolder;
            _logAction = logAction;

            _statusTimer = new System.Windows.Forms.Timer();
            _statusTimer.Interval = 15000;
            _statusTimer.Tick += async (s, e) => await RefreshAsync();
        }

        public void UpdateCurrentFolder(string folder)
        {
            _currentFolder = folder;
        }

        public void SetStatusLabels(Label lblInfinity, Label lblSerenity, Label lblTranquility)
        {
            _lblInfinityStatus = lblInfinity;
            _lblSerenityStatus = lblSerenity;
            _lblTranquilityStatus = lblTranquility;
        }

        public void Start()
        {
            _statusTimer.Start();
            _ = RefreshAsync();
        }

        public void Stop()
        {
            _statusTimer.Stop();
        }

        public async Task RefreshAsync()
        {
            if (_lblInfinityStatus == null || _lblSerenityStatus == null || _lblTranquilityStatus == null)
                return;

            await UpdateServerStatusAsync("infinity", _lblInfinityStatus);
            await UpdateServerStatusAsync("serenity", _lblSerenityStatus);
            await UpdateServerStatusAsync("tq", _lblTranquilityStatus);

            if (_isFirstStatusCheck)
            {
                _isFirstStatusCheck = false;
            }
        }

        private async Task UpdateServerStatusAsync(string serverKey, Label statusLabel)
        {
            string displayName = serverKey == "infinity" ? "曙光服" :
                                 serverKey == "serenity" ? "晨曦服" : "国际服";

            try
            {
                string url = GetStatusUrl(serverKey);
                int timeoutSeconds = (serverKey == "tq") ? 5 : 10;

                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.Add("User-Agent", "EVEConfigManager/1.0");
                    request.Headers.Add("Accept", "application/json");

                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                    {
                        HttpResponseMessage response = await _httpClient.SendAsync(request, cts.Token);

                        if (response.IsSuccessStatusCode)
                        {
                            string json = await response.Content.ReadAsStringAsync();
                            using JsonDocument doc = JsonDocument.Parse(json);
                            int players = doc.RootElement.GetProperty("players").GetInt32();
                            _logAction?.Invoke($"查询{displayName}状态", "成功", $"人数: {players}");

                            bool wasOnline = _previousOnlineStatus.TryGetValue(serverKey, out bool prevOnline) ? prevOnline : false;

                            bool isOnline = players >= 10;

                            _previousOnlineStatus[serverKey] = isOnline;

                            if (isOnline)
                            {
                                statusLabel.Text = $"{displayName}: {players:N0} 人";
                                statusLabel.ForeColor = Color.Green;

                                if (!_isFirstStatusCheck && !wasOnline && !string.IsNullOrEmpty(_currentFolder))
                                {
                                    await NotifyServerOnlineAsync(displayName, players);
                                }
                            }
                            else
                            {
                                string statusText = players == 0 ? "维护中" : "离线";
                                statusLabel.Text = $"{displayName}: {statusText}";
                                statusLabel.ForeColor = Color.Gray;
                            }
                        }
                        else
                        {
                            _previousOnlineStatus[serverKey] = false;
                            statusLabel.Text = $"{displayName}: 离线";
                            statusLabel.ForeColor = Color.Gray;
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                _previousOnlineStatus[serverKey] = false;
                statusLabel.Text = $"{displayName}: 离线";
                statusLabel.ForeColor = Color.Gray;
                _logAction?.Invoke($"查询{displayName}状态", "超时", "请求超时");
            }
            catch (Exception ex)
            {
                _previousOnlineStatus[serverKey] = false;
                statusLabel.Text = $"{displayName}: 查询失败";
                statusLabel.ForeColor = Color.Red;
                _logAction?.Invoke($"查询{displayName}状态", "失败", ex.Message);
            }
        }

        private string GetStatusUrl(string serverKey)
        {
            switch (serverKey)
            {
                case "tq":
                    return "https://esi.evetech.net/latest/status/";
                case "serenity":
                    return "https://ali-esi.evepc.163.com/latest/status/?datasource=serenity";
                case "infinity":
                    return "https://ali-esi.evepc.163.com/latest/status/?datasource=infinity";
                default:
                    return "https://esi.evetech.net/latest/status/";
            }
        }

        private async Task NotifyServerOnlineAsync(string serverName, int players)
        {
            string message = $"{serverName} 已开放！\n当前在线人数: {players:N0} 人";

            await Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Console.Beep(800, 200);
                        Thread.Sleep(200);
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    // 非 Windows 平台忽略蜂鸣
                }
            });

            CustomMessageBox.Show(message, $"{serverName} 上线提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);

            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 5000;
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
                foreach (Form form in Application.OpenForms)
                {
                    if (form is CustomMessageBox)
                    {
                        form.Close();
                    }
                }
            };
            timer.Start();

            _logAction?.Invoke($"{serverName}上线提醒", "成功", $"在线人数: {players}");
        }
    }
}