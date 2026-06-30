using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Monitor.Core.Models;
using Monitor.Hardware.Services;
using Monitor.Network.Services;

namespace Monitor.Worker
{
    public class MainForm : Form
    {
        private readonly IHardwareService _hardwareService;
        private readonly ITcpStreamerService _tcpService;

        // UI labels
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
        private readonly Label _totalWattLabel;
        private readonly Label _lastUpdateLabel;
        private readonly Button _reconnectButton;
        private readonly Button _repairAdbButton;
        private readonly System.Windows.Forms.Timer _buttonVisibilityTimer;

        private bool _exitRequested = false;

        public MainForm(IHardwareService hardwareService, ITcpStreamerService tcpService)
        {
            _hardwareService = hardwareService;
            _tcpService = tcpService;

            // Form sizing and fonts
            Text = "PC Stats Sender"; Width = 320; Height = 510;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen;
            var font = new Font("Segoe UI", 10);

            _statusLabel = new Label { Text = "Status: Starting...", Left = 20, Top = 20, Width = 280, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            _cpuLoadLabel = new Label { Text = "CPU Load: -", Left = 20, Top = 50, Width = 280, Font = font };
            _cpuTempLabel = new Label { Text = "CPU Temp: -", Left = 20, Top = 80, Width = 280, Font = font };
            _cpuClockLabel = new Label { Text = "CPU Clock: -", Left = 20, Top = 110, Width = 280, Font = font };
            _cpuWattLabel = new Label { Text = "CPU Power: -", Left = 20, Top = 140, Width = 280, Font = font };
            _gpuLoadLabel = new Label { Text = "GPU Load: -", Left = 20, Top = 175, Width = 280, Font = font };
            _gpuTempLabel = new Label { Text = "GPU Temp: -", Left = 20, Top = 205, Width = 280, Font = font };
            _gpuClockLabel = new Label { Text = "GPU Clock: -", Left = 20, Top = 235, Width = 280, Font = font };
            _gpuWattLabel = new Label { Text = "GPU Power: -", Left = 20, Top = 265, Width = 280, Font = font };
            _ramUsageLabel = new Label { Text = "RAM Usage: -", Left = 20, Top = 300, Width = 280, Font = font };
            _totalWattLabel = new Label { Text = "TOTAL POWER: -", Left = 20, Top = 330, Width = 280, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.DeepSkyBlue };
            _lastUpdateLabel = new Label { Text = "Last update: -", Left = 20, Top = 365, Width = 280, ForeColor = Color.Gray };

            _reconnectButton = new Button { Text = "Reconnect", Left = 20, Top = 390, Width = 280, Height = 28, Visible = false };
            _reconnectButton.Click += (s, e) => Reconnect();

            _repairAdbButton = new Button { Text = "Repair ADB Tools", Left = 20, Top = 425, Width = 280, Height = 28, Visible = false };
            _repairAdbButton.Click += async (s, e) => await RepairAdbAsync();

            Controls.AddRange(new Control[] { _statusLabel, _cpuLoadLabel, _cpuTempLabel, _cpuClockLabel, _cpuWattLabel, _gpuLoadLabel, _gpuTempLabel, _gpuClockLabel, _gpuWattLabel, _ramUsageLabel, _totalWattLabel, _lastUpdateLabel, _reconnectButton, _repairAdbButton });

            // Polls the actual connection/ADB state once a second and shows
            // each button only while its corresponding problem exists, so
            // the window stays clean when everything is working normally.
            _buttonVisibilityTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _buttonVisibilityTimer.Tick += (s, e) => UpdateButtonVisibility();
            _buttonVisibilityTimer.Start();

            // Tray menu
            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show", null, (s, e) => ShowFromTray());
            trayMenu.Items.Add("Reconnect", null, (s, e) => Reconnect());
            trayMenu.Items.Add("Exit", null, (s, e) => ExitApp());

            _trayIcon = new NotifyIcon { Icon = SystemIcons.Information, Text = "PC Stats Sender", Visible = true, ContextMenuStrip = trayMenu };
            _trayIcon.DoubleClick += (s, e) => ShowFromTray();

            Resize += (s, e) => { if (WindowState == FormWindowState.Minimized) { Hide(); ShowInTaskbar = false; } };
            FormClosing += (s, e) => { if (!_exitRequested) { e.Cancel = true; Hide(); ShowInTaskbar = false; } };

            // Win+L lock/unlock automation
            Microsoft.Win32.SystemEvents.SessionSwitch += (s, e) =>
            {
                if (e.Reason == Microsoft.Win32.SessionSwitchReason.SessionLock) _tcpService.TriggerAdbControl(false);
                else if (e.Reason == Microsoft.Win32.SessionSwitchReason.SessionUnlock) _tcpService.TriggerAdbControl(true);
            };

            // Forward connection status updates from the network service to the UI
            _tcpService.OnStatusChanged += (msg, isSuccess) => UpdateStatus(msg, isSuccess ? Color.Green : Color.OrangeRed);

            // IMPORTANT: start hidden in the system tray. Without this the form
            // opens visibly on launch (this was the cause of the "not starting
            // hidden" bug after the architecture refactor).
            Load += (s, e) =>
            {
                Hide();
                ShowInTaskbar = false;
                UpdateButtonVisibility();

                // If adb.exe is missing (deleted platform-tools folder, fresh
                // install without it, etc.) a status label change alone would
                // go unnoticed since the app starts hidden. Surface this with
                // a tray balloon AND open the window directly on the Repair
                // button so the user can fix it with a single click.
                if (!_tcpService.IsAdbAvailable())
                {
                    UpdateStatus("ADB not found - click \"Repair ADB Tools\"", Color.OrangeRed);

                    _trayIcon.ShowBalloonTip(
                        8000,
                        "ADB Tools Missing",
                        "The platform-tools folder was not found. Click here to open PC Stats Sender and repair it automatically.",
                        ToolTipIcon.Warning);

                    ShowFromTray();
                    _repairAdbButton.Focus();
                }
            };

            // Clicking the balloon notification also opens the window, in
            // case the user dismissed the auto-opened window beforehand.
            _trayIcon.BalloonTipClicked += (s, e) => ShowFromTray();
        }

        private void ShowFromTray() { ShowInTaskbar = true; Show(); WindowState = FormWindowState.Normal; Activate(); }

        // Shows each button only while its corresponding problem actually
        // exists: Reconnect when there is no active TCP connection, Repair
        // ADB Tools when adb.exe can't be found. Called once at startup and
        // then every second by _buttonVisibilityTimer.
        private void UpdateButtonVisibility()
        {
            if (IsDisposed) return;
            _reconnectButton.Visible = !_tcpService.IsConnected;
            _repairAdbButton.Visible = !_tcpService.IsAdbAvailable();
        }

        // Forces the current TCP connection (if any) to drop and restarts
        // the local adb server, so a physical cable unplug/replug is
        // recovered from properly instead of leaving the adb daemon with a
        // stale view of the device. Worker's NetworkConnectLoop notices the
        // drop via IsConnected and reconnects automatically afterwards.
        private void Reconnect()
        {
            UpdateStatus("Reconnecting...", Color.OrangeRed);
            _tcpService.ForceReconnect();
        }

        // Downloads and re-extracts platform-tools next to the .exe, in
        // case the "platform-tools" folder was deleted, moved, or never
        // installed. Disables the button while running so it can't be
        // triggered twice at once.
        private async Task RepairAdbAsync()
        {
            _repairAdbButton.Enabled = false;
            try
            {
                var success = await _tcpService.RepairAdbAsync();
                if (success) Reconnect(); // immediately retry the connection with the fixed adb
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
            BeginInvoke((Action)(() => { _statusLabel.Text = "Status: " + text; _statusLabel.ForeColor = color; }));
        }

        public void UpdateUiValues(PcStatsDto stats)
        {
            if (IsDisposed) return;
            BeginInvoke((Action)(() =>
            {
                _cpuLoadLabel.Text = $"CPU Load: {stats.CpuLoad:0}%";
                _cpuTempLabel.Text = $"CPU Temp: {stats.CpuTemp:0}°C";
                _cpuClockLabel.Text = $"CPU Clock: {stats.CpuClock:0} MHz";
                _cpuWattLabel.Text = $"CPU Power: {stats.CpuWatt:0.0} W";
                _gpuLoadLabel.Text = $"GPU Load: {stats.GpuLoad:0}%";
                _gpuTempLabel.Text = $"GPU Temp: {stats.GpuTemp:0}°C";
                _gpuClockLabel.Text = $"GPU Clock: {stats.GpuClock:0} MHz";
                _gpuWattLabel.Text = $"GPU Power: {stats.GpuWatt:0.0} W";
                _ramUsageLabel.Text = $"RAM Usage: {stats.RamUsage:0.0}%";
                _totalWattLabel.Text = $"TOTAL POWER: {stats.TotalWatt:0.0} W";
                _lastUpdateLabel.Text = "Last update: " + DateTime.Now.ToString("HH:mm:ss");
            }));
        }
    }
}