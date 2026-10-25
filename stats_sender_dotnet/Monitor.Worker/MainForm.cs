using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Monitor.Core.Models;
using Monitor.Hardware.Services;
using Monitor.Network.Services;
using Monitor.Worker.Rgb;

namespace Monitor.Worker
{
    public class MainForm : Form
    {
        private readonly IHardwareService _hardwareService;
        private readonly ITcpStreamerService _tcpService;
        private readonly SerialFanSender _serialFanSender;
        private readonly RgbService _rgb;
        private readonly System.Collections.Generic.Dictionary<RgbService.Target, Label> _rgbStatusLabels = new();

        private const string StartupRegistryKey   = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupRegistryValue = "PcStatsMonitor";

        private readonly NotifyIcon  _trayIcon;
        private readonly Label       _statusLabel;
        
        // --- CPU LABELS ---
        private readonly Label       _cpuLoadLabel;
        private readonly Label       _cpuTempLabel;
        private readonly Label       _cpuClockLabel;
        private readonly Label       _cpuWattLabel;
        private readonly Label       _cpuFanLabel;
        private readonly Label       _cpuOptFanLabel;
        private readonly Label       _cpuPumpLabel;
        private readonly Label       _cpuVcoreLabel;

        // --- MOTHERBOARD / VRM LABELS ---
        private readonly Label       _vrmMosTempLabel;
        private readonly Label       _vrmSocTempLabel;

        // --- GPU LABELS ---
        private readonly Label       _gpuLoadLabel;
        private readonly Label       _gpuTempLabel;
        private readonly Label       _gpuHotspotTempLabel;
        private readonly Label       _gpuJunctionTempLabel;
        private readonly Label       _gpuClockLabel;
        private readonly Label       _gpuWattLabel;
        private readonly Label       _gpuCorePowerLabel;
        private readonly Label       _gpuBoardPowerLabel;
        private readonly Label       _gpuFan1Label;
        private readonly Label       _gpuFan2Label;

        // --- RAM & SYSTEM LABELS ---
        private readonly Label       _ramUsageLabel;
        private readonly Label       _ramUsageGBLabel;
        private readonly Label       _ramTotalGBLabel;
        private readonly Label       _totalWattLabel;
        private readonly Label       _lastUpdateLabel;
        private readonly Label       _arduinoStatusLabel;
        
        private readonly CheckBox    _startupCheckBox;
        private readonly Button      _reconnectButton;
        private readonly Button      _repairAdbButton;
        private readonly System.Windows.Forms.Timer _buttonVisibilityTimer;

        private bool _exitRequested = false;

        public MainForm(IHardwareService hardwareService, ITcpStreamerService tcpService, SerialFanSender serialFanSender, RgbService rgb)
        {
            _hardwareService = hardwareService;
            _tcpService      = tcpService;
            _serialFanSender = serialFanSender;
            _rgb             = rgb;

            Text = "PC Stats Sender"; Width = 700; Height = 1010;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            var font = new Font("Segoe UI", 9.5f);

            int top = 20;
            int step = 26;

            _statusLabel          = new Label { Text = "Status: Starting...", Left = 20, Top = top, Width = 280, Font = new Font("Segoe UI", 10, FontStyle.Bold) }; top += step + 5;
            
            // CPU
            _cpuLoadLabel         = new Label { Text = "CPU Load: -",         Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _cpuTempLabel         = new Label { Text = "CPU Temp: -",         Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _cpuClockLabel        = new Label { Text = "CPU Clock: -",        Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _cpuWattLabel         = new Label { Text = "CPU Power: -",        Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _cpuFanLabel          = new Label { Text = "CPU Fan: -",          Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _cpuOptFanLabel       = new Label { Text = "CPU OPT Fan: -",      Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _cpuPumpLabel         = new Label { Text = "CPU Pump: -",         Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _cpuVcoreLabel        = new Label { Text = "CPU VCore: -",        Left = 20, Top = top, Width = 280, Font = font }; top += step + 5;

            // VRM / ANAKART
            _vrmMosTempLabel      = new Label { Text = "VRM MOS Temp: -",     Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _vrmSocTempLabel      = new Label { Text = "VRM SOC Temp: -",     Left = 20, Top = top, Width = 280, Font = font }; top += step + 5;

            // GPU
            _gpuLoadLabel         = new Label { Text = "GPU Load: -",         Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _gpuTempLabel         = new Label { Text = "GPU Temp: -",         Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _gpuHotspotTempLabel  = new Label { Text = "GPU Hot Spot: -",     Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _gpuJunctionTempLabel = new Label { Text = "GPU Junction: -",     Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _gpuClockLabel        = new Label { Text = "GPU Clock: -",        Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _gpuWattLabel         = new Label { Text = "GPU Power: -",        Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _gpuCorePowerLabel    = new Label { Text = "GPU Core Power: -",   Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _gpuBoardPowerLabel   = new Label { Text = "GPU Board Power: -",  Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _gpuFan1Label         = new Label { Text = "GPU Fan 1: -",        Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _gpuFan2Label         = new Label { Text = "GPU Fan 2: -",        Left = 20, Top = top, Width = 280, Font = font }; top += step + 5;

            // RAM & GÜÇ
            _ramUsageLabel        = new Label { Text = "RAM Usage: -",        Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _ramUsageGBLabel      = new Label { Text = "RAM Used: -",         Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _ramTotalGBLabel      = new Label { Text = "RAM Total: -",        Left = 20, Top = top, Width = 280, Font = font }; top += step;
            _totalWattLabel       = new Label { Text = "TOTAL POWER: -",      Left = 20, Top = top, Width = 280, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.DeepSkyBlue }; top += step + 5;
            _lastUpdateLabel      = new Label { Text = "Last update: -",      Left = 20, Top = top, Width = 280, ForeColor = Color.Gray }; top += step;
            _arduinoStatusLabel   = new Label { Text = "Arduino: -",          Left = 20, Top = top, Width = 280, ForeColor = Color.Gray }; top += step + 10;

            _startupCheckBox = new CheckBox
            {
                Text = "Start with Windows",
                Left = 20, Top = top, Width = 280, Font = font,
                Checked = IsStartupEnabled(),
            }; top += step + 5;

            _reconnectButton = new Button { Text = "Reconnect",        Left = 20, Top = top, Width = 280, Height = 28, Visible = false }; top += 32;
            _repairAdbButton = new Button { Text = "Repair ADB Tools", Left = 20, Top = top, Width = 280, Height = 28, Visible = false };

            Controls.AddRange(new Control[]
            {
                _statusLabel, 
                _cpuLoadLabel, _cpuTempLabel, _cpuClockLabel, _cpuWattLabel, _cpuFanLabel, _cpuOptFanLabel,_cpuPumpLabel, _cpuVcoreLabel,
                _vrmMosTempLabel, _vrmSocTempLabel,
                _gpuLoadLabel, _gpuTempLabel, _gpuHotspotTempLabel, _gpuJunctionTempLabel, _gpuClockLabel, _gpuWattLabel, _gpuCorePowerLabel, _gpuBoardPowerLabel, _gpuFan1Label, _gpuFan2Label,
                _ramUsageLabel, _ramUsageGBLabel, _ramTotalGBLabel,
                _totalWattLabel, _lastUpdateLabel, _arduinoStatusLabel,
                _startupCheckBox, _reconnectButton, _repairAdbButton,
            });

            _buttonVisibilityTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _buttonVisibilityTimer.Tick += (s, e) => UpdateButtonVisibility();
            _buttonVisibilityTimer.Start();

            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show",      null, (s, e) => ShowFromTray());
            trayMenu.Items.Add("Reconnect", null, (s, e) => Reconnect());
            trayMenu.Items.Add("Exit",      null, (s, e) => ExitApp());

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Text = "PC Stats Sender",
                Visible = true,
                ContextMenuStrip = trayMenu,
            };
            _trayIcon.DoubleClick += (s, e) => ShowFromTray();

            Resize += (s, e) =>
            {
                if (WindowState == FormWindowState.Minimized) { Hide(); ShowInTaskbar = false; }
            };

            FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing && !_exitRequested)
                {
                    e.Cancel = true;
                    Hide();
                    ShowInTaskbar = false;
                }
            };

            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.SessionEnding += OnSessionEnding;

            _tcpService.OnStatusChanged += (msg, isSuccess) =>
                UpdateStatus(msg, isSuccess ? Color.Green : Color.OrangeRed);

            _serialFanSender.OnStatusChanged += (msg, isSuccess) =>
                UpdateArduinoStatus(msg, isSuccess ? Color.Green : Color.OrangeRed);

            Load += (s, e) =>
            {
                Hide();
                ShowInTaskbar = false;
                UpdateButtonVisibility();

                if (!_tcpService.IsAdbAvailable())
                {
                    UpdateStatus("ADB not found - click \"Repair ADB Tools\"", Color.OrangeRed);
                    _trayIcon.ShowBalloonTip(8000, "ADB Tools Missing",
                        "The platform-tools folder was not found. Click here to open PC Stats Sender and repair it automatically.",
                        ToolTipIcon.Warning);
                    ShowFromTray();
                    _repairAdbButton.Focus();
                }
            };

            _trayIcon.BalloonTipClicked += (s, e) => ShowFromTray();

            BuildRgbPanel();
        }

        // ── RGB panel ─────────────────────────────────────────────────────────

        private void BuildRgbPanel()
        {
            const int x = 340;
            int y = 20;

            Controls.Add(new Label
            {
                Text = "RGB & Screen Colors", Left = x, Top = y, Width = 320,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
            });
            y += 34;

            AddRgbRow("Keyboard (Corsair K68 RGB)",      RgbService.Target.Keyboard, x, ref y);
          // AddRgbRow("Mouse (SteelSeries Rival 3)",     RgbService.Target.Mouse,    x, ref y);
          // AddRgbRow("GPU (ASUS TUF)",                  RgbService.Target.Gpu,      x, ref y);
          // AddRgbRow("Motherboard (Gigabyte X370)",     RgbService.Target.Motherboard, x, ref y); // Anakart Eklendi
           AddRgbRow("Android App Screen Color",        RgbService.Target.Screen,   x, ref y); // Screen Color satırı

            _rgb.StatusChanged += (target, message, ok) =>
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke((Action)(() => SetRgbStatus(target, message, ok)));
            };

            Load += (s, e) =>
            {
                foreach (RgbService.Target t in Enum.GetValues(typeof(RgbService.Target)))
                {
                    var st = _rgb.GetStatus(t);
                    if (st.HasValue) SetRgbStatus(t, st.Value.Message, st.Value.Ok);
                }
            };
        }

        private void AddRgbRow(string title, RgbService.Target target, int x, ref int y)
        {
            var color = _rgb.GetColor(target);

            var titleLabel = new Label { Text = title, Left = x, Top = y, Width = 320, Font = new Font("Segoe UI", 9.5f) };
            var button = new Button
            {
                Left = x, Top = y + 24, Width = 320, Height = 32,
                FlatStyle = FlatStyle.Flat,
                Text = RgbSettings.ToHex(color),
                BackColor = color,
                ForeColor = ContrastColor(color),
            };
            var statusLabel = new Label
            {
                Text = target == RgbService.Target.Screen ? "Active" : "Waiting...", 
                Left = x, Top = y + 60, Width = 320, Height = 34,
                ForeColor = target == RgbService.Target.Screen ? Color.Green : Color.Gray,
            };
            _rgbStatusLabels[target] = statusLabel;

            button.Click += async (s, e) =>
            {
                using var dlg = new ColorDialog { Color = _rgb.GetColor(target), FullOpen = true, AnyColor = true };
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var c = dlg.Color;
                button.BackColor = c;
                button.ForeColor = ContrastColor(c);
                button.Text = RgbSettings.ToHex(c);
                await _rgb.SetColorAsync(target, c);
            };

            Controls.AddRange(new Control[] { titleLabel, button, statusLabel });
            y += 104;
        }

        private void SetRgbStatus(RgbService.Target target, string message, bool ok)
        {
            if (!_rgbStatusLabels.TryGetValue(target, out var label)) return;
            label.Text = message;
            label.ForeColor = ok ? Color.Green : Color.OrangeRed;
        }

        private static Color ContrastColor(Color c) =>
            (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) > 150 ? Color.Black : Color.White;

        // ── Dispose ───────────────────────────────────────────────────────────

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { SystemEvents.SessionSwitch -= OnSessionSwitch; } catch { }
                try { SystemEvents.SessionEnding -= OnSessionEnding; } catch { }

                _buttonVisibilityTimer.Stop();
                _buttonVisibilityTimer.Dispose();
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            base.Dispose(disposing);
        }

        // ── Session events ────────────────────────────────────────────────────

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)   _tcpService.TriggerAdbControl(false);
            if (e.Reason == SessionSwitchReason.SessionUnlock) _tcpService.TriggerAdbControl(true);
        }

        private void OnSessionEnding(object sender, SessionEndingEventArgs e)
        {
            try { _tcpService.TriggerAdbControl(false); } catch { }
        }

        // ── Startup registry ──────────────────────────────────────────────────

        private static bool IsStartupEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: false);
            if (key == null) return false;
            var value = key.GetValue(StartupRegistryValue);
            if (value == null) return false;
            var expected = $"\"{System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName}\"";
            return string.Equals(value.ToString(), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static void SetStartup(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: true);
            if (key == null) return;
            if (enable)
            {
                var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                key.SetValue(StartupRegistryValue, $"\"{exe}\"");
            }
            else
            {
                key.DeleteValue(StartupRegistryValue, throwOnMissingValue: false);
            }
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        private void ShowFromTray()
        {
            ShowInTaskbar = true; Show(); WindowState = FormWindowState.Normal; Activate();
        }

        private void UpdateButtonVisibility()
        {
            if (IsDisposed) return;
            _reconnectButton.Visible = !_tcpService.IsConnected;
            _repairAdbButton.Visible = !_tcpService.IsAdbAvailable();
        }

        private void Reconnect()
        {
            UpdateStatus("Reconnecting...", Color.OrangeRed);
            _tcpService.ForceReconnect();
        }

        private async Task RepairAdbAsync()
        {
            _repairAdbButton.Enabled = false;
            try { var ok = await _tcpService.RepairAdbAsync(); if (ok) Reconnect(); }
            finally { _repairAdbButton.Enabled = true; }
        }

        private void ExitApp()
        {
            _exitRequested = true;
            _tcpService.TriggerAdbControl(false);
            _tcpService.Disconnect();
            _hardwareService.Shutdown();
            Application.Exit();
        }

        private void UpdateStatus(string text, Color color)
        {
            if (IsDisposed) return;
            BeginInvoke((Action)(() =>
            {
                _statusLabel.Text      = "Status: " + text;
                _statusLabel.ForeColor = color;
            }));
        }

        private void UpdateArduinoStatus(string text, Color color)
        {
            if (IsDisposed) return;
            BeginInvoke((Action)(() =>
            {
                _arduinoStatusLabel.Text      = "Arduino: " + text;
                _arduinoStatusLabel.ForeColor = color;
            }));
        }

        public void UpdateUiValues(PcStatsDto stats)
        {
            if (IsDisposed) return;
            BeginInvoke((Action)(() =>
            {
                _cpuLoadLabel.Text         = $"CPU Load: {stats.CpuLoad:0}%";
                _cpuTempLabel.Text         = $"CPU Temp: {stats.CpuTemp:0}°C";
                _cpuClockLabel.Text        = $"CPU Clock: {stats.CpuClock:0} MHz";
                _cpuWattLabel.Text         = $"CPU Power: {stats.CpuWatt:0.0} W";
                _cpuFanLabel.Text          = $"CPU Fan: {stats.CpuFanRpm:0} RPM";
                _cpuOptFanLabel.Text       = $"CPU OPT Fan: {stats.CpuOptFanRpm:0} RPM";
                _cpuVcoreLabel.Text        = $"CPU VCore: {stats.CpuVcore:0.000} V";
                _cpuPumpLabel.Text         = $"CPU Pump: {stats.CpuPumpRpm:0} RPM";
                
                _vrmMosTempLabel.Text      = $"VRM MOS Temp: {stats.VrmMosTemp:0}°C";
                _vrmSocTempLabel.Text      = $"VRM SOC Temp: {stats.VrmSocTemp:0}°C";

                _gpuLoadLabel.Text         = $"GPU Load: {stats.GpuLoad:0}%";
                _gpuTempLabel.Text         = $"GPU Temp: {stats.GpuTemp:0}°C";
                _gpuHotspotTempLabel.Text  = $"GPU Hot Spot: {stats.GpuHotspotTemp:0}°C";
                _gpuJunctionTempLabel.Text = $"GPU Junction: {stats.GpuJunctionTemp:0}°C";
                _gpuClockLabel.Text        = $"GPU Clock: {stats.GpuClock:0} MHz";
                _gpuWattLabel.Text         = $"GPU Power: {stats.GpuWatt:0.0} W";
                _gpuCorePowerLabel.Text    = $"GPU Core Power: {stats.GpuCorePower:0.0} W";
                _gpuBoardPowerLabel.Text   = $"GPU Board Power: {stats.GpuBoardPower:0.0} W";
                _gpuFan1Label.Text         = $"GPU Fan 1: {stats.GpuFan1Percentage:0}% ({stats.GpuFan1Rpm:0} RPM)";
                _gpuFan2Label.Text         = $"GPU Fan 2: {stats.GpuFan2Percentage:0}% ({stats.GpuFan2Rpm:0} RPM)";

                _ramUsageLabel.Text        = $"RAM Usage: {stats.RamUsage:0.0}%";
                _ramUsageGBLabel.Text      = $"RAM Used: {stats.RamUsedGb:0.0} GB";
                _ramTotalGBLabel.Text      = $"RAM Total: {stats.RamTotalGb:0.0} GB";
                _totalWattLabel.Text       = $"TOTAL POWER: {stats.TotalWatt:0.0} W";
                _lastUpdateLabel.Text      = "Last update: " + DateTime.Now.ToString("HH:mm:ss");
            }));
        }
    }
}