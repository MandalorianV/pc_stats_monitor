using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Monitor.Core.Models;
using Monitor.Hardware.Services;
using Monitor.Network.Services;

namespace Monitor.Worker
{
    public class MainForm : Form
    {
        private readonly IHardwareService _hardwareService;
        private readonly ITcpStreamerService _tcpService;

        // Registry key used for Windows startup entries (current user, no admin needed)
        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupRegistryValue = "PcStatsMonitor";

        // UI controls
        private readonly NotifyIcon _trayIcon;
        private readonly Label _statusLabel;
        private readonly Label _cpuLoadLabel;
        private readonly Label _cpuTempLabel;
        private readonly Label _cpuClockLabel;
        private readonly Label _cpuWattLabel;
        private readonly Label _gpuLoadLabel;
        private readonly Label _gpuTempLabel;
        private readonly Label _gpuClockLabel;
        private readonly Label _gpuWattLabel;
        private readonly Label _ramUsageLabel;

        private readonly Label _ramUsageGBLabel;

        private readonly Label _ramTotalGBLabel;
        private readonly Label _totalWattLabel;
        private readonly Label _lastUpdateLabel;
        private readonly CheckBox _startupCheckBox;
        private readonly Button _reconnectButton;
        private readonly Button _repairAdbButton;
        private readonly System.Windows.Forms.Timer _buttonVisibilityTimer;

        private bool _exitRequested = false;

        public MainForm(IHardwareService hardwareService, ITcpStreamerService tcpService)
        {
            _hardwareService = hardwareService;
            _tcpService = tcpService;

            Text = "PC Stats Sender"; Width = 320; Height = 600;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen;
            var font = new Font("Segoe UI", 10);

            _statusLabel     = new Label { Text = "Status: Starting...", Left = 20, Top = 20,  Width = 280, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            _cpuLoadLabel    = new Label { Text = "CPU Load: -",         Left = 20, Top = 50,  Width = 280, Font = font };
            _cpuTempLabel    = new Label { Text = "CPU Temp: -",         Left = 20, Top = 80,  Width = 280, Font = font };
            _cpuClockLabel   = new Label { Text = "CPU Clock: -",        Left = 20, Top = 110, Width = 280, Font = font };
            _cpuWattLabel    = new Label { Text = "CPU Power: -",        Left = 20, Top = 140, Width = 280, Font = font };
            _gpuLoadLabel    = new Label { Text = "GPU Load: -",         Left = 20, Top = 175, Width = 280, Font = font };
            _gpuTempLabel    = new Label { Text = "GPU Temp: -",         Left = 20, Top = 205, Width = 280, Font = font };
            _gpuClockLabel   = new Label { Text = "GPU Clock: -",        Left = 20, Top = 235, Width = 280, Font = font };
            _gpuWattLabel    = new Label { Text = "GPU Power: -",        Left = 20, Top = 265, Width = 280, Font = font };
            _ramUsageLabel   = new Label { Text = "RAM Usage: -",        Left = 20, Top = 300, Width = 280, Font = font };
            _ramUsageGBLabel = new Label { Text = "RAM Usage GB: -",        Left = 20, Top = 330, Width = 280, Font = font };
            _ramTotalGBLabel = new Label { Text = "RAM Total GB: -",        Left = 20, Top = 360, Width = 280, Font = font };
            _totalWattLabel  = new Label { Text = "TOTAL POWER: -",      Left = 20, Top = 390, Width = 280, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.DeepSkyBlue };
            _lastUpdateLabel = new Label { Text = "Last update: -",      Left = 20, Top = 420, Width = 280, ForeColor = Color.Gray };

            // Reads the current registry state so the checkbox always reflects reality.
            _startupCheckBox = new CheckBox
            {
                Text = "Start with Windows",
                Left = 20, Top = 450, Width = 280,
                Font = font,
                Checked = IsStartupEnabled(),
            };
            _startupCheckBox.CheckedChanged += (s, e) => SetStartup(_startupCheckBox.Checked);

            _reconnectButton = new Button { Text = "Reconnect",        Left = 20, Top = 480, Width = 280, Height = 28, Visible = false };
            _reconnectButton.Click += (s, e) => Reconnect();

            _repairAdbButton = new Button { Text = "Repair ADB Tools", Left = 20, Top = 510, Width = 280, Height = 28, Visible = false };
            _repairAdbButton.Click += async (s, e) => await RepairAdbAsync();

            Controls.AddRange(new Control[]
            {
                _statusLabel, _cpuLoadLabel, _cpuTempLabel, _cpuClockLabel, _cpuWattLabel,
                _gpuLoadLabel, _gpuTempLabel, _gpuClockLabel, _gpuWattLabel,
                _ramUsageLabel,_ramUsageGBLabel,_ramTotalGBLabel, _totalWattLabel, _lastUpdateLabel,
                _startupCheckBox, _reconnectButton, _repairAdbButton,
            });

            _buttonVisibilityTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _buttonVisibilityTimer.Tick += (s, e) => UpdateButtonVisibility();
            _buttonVisibilityTimer.Start();

            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show",      null, (s, e) => ShowFromTray());
            trayMenu.Items.Add("Reconnect", null, (s, e) => Reconnect());
            trayMenu.Items.Add("Exit",      null, (s, e) => ExitApp());

            _trayIcon = new NotifyIcon { Icon = SystemIcons.Information, Text = "PC Stats Sender", Visible = true, ContextMenuStrip = trayMenu };
            _trayIcon.DoubleClick += (s, e) => ShowFromTray();

            Resize      += (s, e) => { if (WindowState == FormWindowState.Minimized) { Hide(); ShowInTaskbar = false; } };
            FormClosing += (s, e) => { if (!_exitRequested) { e.Cancel = true; Hide(); ShowInTaskbar = false; } };

            SystemEvents.SessionSwitch += (s, e) =>
            {
                if (e.Reason == SessionSwitchReason.SessionLock)   _tcpService.TriggerAdbControl(false);
                if (e.Reason == SessionSwitchReason.SessionUnlock) _tcpService.TriggerAdbControl(true);
            };

            SystemEvents.SessionEnding += (s, e) => _tcpService.TriggerAdbControl(false);

            _tcpService.OnStatusChanged += (msg, isSuccess) =>
                UpdateStatus(msg, isSuccess ? Color.Green : Color.OrangeRed);

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
        }

        // ── Startup registry ──────────────────────────────────────────────────

        private static bool IsStartupEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: false);
            return key?.GetValue(StartupRegistryValue) != null;
        }

        private static void SetStartup(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: true);
            if (key == null) return;

            if (enable)
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                              ?? string.Empty;
                key.SetValue(StartupRegistryValue, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(StartupRegistryValue, throwOnMissingValue: false);
            }
        }

        // ── UI ────────────────────────────────────────────────────────────────

        private void ShowFromTray()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
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
            try
            {
                var success = await _tcpService.RepairAdbAsync();
                if (success) Reconnect();
            }
            finally
            {
                _repairAdbButton.Enabled = true;
            }
        }

        private void ExitApp()
        {
            _exitRequested = true;
            _buttonVisibilityTimer.Stop();
            _trayIcon.Visible = false;
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
                _statusLabel.Text = "Status: " + text;
                _statusLabel.ForeColor = color;
            }));
        }

        public void UpdateUiValues(PcStatsDto stats)
        {
            if (IsDisposed) return;
            BeginInvoke((Action)(() =>
            {
                _cpuLoadLabel.Text    = $"CPU Load: {stats.CpuLoad:0}%";
                _cpuTempLabel.Text    = $"CPU Temp: {stats.CpuTemp:0}°C";
                _cpuClockLabel.Text   = $"CPU Clock: {stats.CpuClock:0} MHz";
                _cpuWattLabel.Text    = $"CPU Power: {stats.CpuWatt:0.0} W";
                _gpuLoadLabel.Text    = $"GPU Load: {stats.GpuLoad:0}%";
                _gpuTempLabel.Text    = $"GPU Temp: {stats.GpuTemp:0}°C";
                _gpuClockLabel.Text   = $"GPU Clock: {stats.GpuClock:0} MHz";
                _gpuWattLabel.Text    = $"GPU Power: {stats.GpuWatt:0.0} W";
                _ramUsageLabel.Text   = $"RAM Usage: {stats.RamUsage:0.0}%";
                _ramUsageGBLabel.Text = $"RAM Usage GB: {stats.RamUsedGb:0.0}GB";
                _ramTotalGBLabel.Text = $"RAM Total GB: {stats.RamTotalGb:0.0}GB";
                _totalWattLabel.Text  = $"TOTAL POWER: {stats.TotalWatt:0.0} W";
                _lastUpdateLabel.Text = "Last update: " + DateTime.Now.ToString("HH:mm:ss");
            }));
        }
    }
}