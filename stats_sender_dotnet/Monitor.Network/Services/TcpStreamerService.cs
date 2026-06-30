using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Monitor.Core;
using Monitor.Core.Models;

namespace Monitor.Network.Services
{
    public class TcpStreamerService : ITcpStreamerService
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private const string AppPackageName = "com.example.pc_stats_monitor";

        // TcpClient.Connected is unreliable for our purposes: it only
        // reflects the result of the *last I/O operation*, not the actual
        // current state, so it can still report true right after we close
        // the socket ourselves (e.g. via the Reconnect button). We track
        // the connection state explicitly instead.
        private volatile bool _isConnected;

        public event Action<string, bool>? OnStatusChanged;

        public bool IsConnected => _isConnected;

        public async Task ConnectAsync(string ip, int port)
        {
            // Re-establish the ADB port forward before every connection
            // attempt. This replaces the old start_stats.bat/.vbs scripts:
            // if no phone is plugged in yet this simply fails silently and
            // the next retry (a few seconds later) tries again, so the user
            // only needs the .exe + a "platform-tools" folder next to it -
            // no external scripts or Task Scheduler wrapper needed.
            EnsureAdbForward(port);

            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ip, port);
                _stream = _client.GetStream();
                _isConnected = true;

                OnStatusChanged?.Invoke("Connected (phone)", true);
                TriggerAdbControl(true);
            }
            catch
            {
                _isConnected = false;
                OnStatusChanged?.Invoke("Phone not connected (sensors still reading)", false);
                throw;
            }
        }

        private void EnsureAdbForward(int port)
        {
            ExecuteAdbCommand($"forward tcp:{port} tcp:{port}");
        }

        public async Task SendStatsAsync(PcStatsDto stats)
        {
            if (!_isConnected || _stream == null) return;

            try
            {
                var json = JsonSerializer.Serialize(stats, SourceGenerationContext.Default.PcStatsDto) + "\n";
                var data = Encoding.UTF8.GetBytes(json);
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch
            {
                OnStatusChanged?.Invoke("Connection lost, reconnecting...", false);
                Disconnect();
            }
        }

        public void TriggerAdbControl(bool wakeUp)
        {
            if (wakeUp)
            {
                ExecuteAdbCommand("shell input keyevent KEYCODE_WAKEUP");
                ExecuteAdbCommand("shell input swipe 500 1500 500 500 150");
                ExecuteAdbCommand($"shell monkey -p {AppPackageName} -c android.intent.category.LAUNCHER 1");
            }
            else
            {
                ExecuteAdbCommand("shell input keyevent KEYCODE_SLEEP");
            }
        }

        private void ExecuteAdbCommand(string arguments)
        {
            try
            {
                string adbPath = FindAdbPath();

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                process?.WaitForExit(2000);
            }
            catch { }
        }

        // Tries a few known locations for adb.exe before falling back to PATH.
        private static string FindAdbPath()
        {
            var candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "platform-tools", "adb.exe");
            if (File.Exists(candidate)) return candidate;

            candidate = @"C:\platform-tools\adb.exe";
            if (File.Exists(candidate)) return candidate;

            return "adb.exe"; // relies on PATH if neither fixed location exists
        }

        // True only if adb.exe was actually found at one of the known
        // locations (not the bare "adb.exe" PATH fallback, which we can't
        // verify without trying to run it).
        public bool IsAdbAvailable()
        {
            var path = FindAdbPath();
            return path != "adb.exe" && File.Exists(path);
        }

        // Downloads Google's official "always latest" platform-tools build
        // and extracts it next to the .exe, so the user only ever needs the
        // .exe itself - no manual zip download/extract required even if the
        // platform-tools folder gets deleted or never existed.
        private const string PlatformToolsUrl =
            "https://dl.google.com/android/repository/platform-tools-latest-windows.zip";

        public async Task<bool> RepairAdbAsync()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var zipPath = Path.Combine(baseDir, "platform-tools-latest.zip");

            try
            {
                OnStatusChanged?.Invoke("Downloading platform-tools...", false);

                using (var http = new HttpClient())
                {
                    var bytes = await http.GetByteArrayAsync(PlatformToolsUrl);
                    await File.WriteAllBytesAsync(zipPath, bytes);
                }

                OnStatusChanged?.Invoke("Extracting platform-tools...", false);

                var targetDir = Path.Combine(baseDir, "platform-tools");
                if (Directory.Exists(targetDir))
                    Directory.Delete(targetDir, recursive: true);

                // The zip already contains a top-level "platform-tools" folder,
                // so extracting into baseDir produces baseDir/platform-tools/*.
                ZipFile.ExtractToDirectory(zipPath, baseDir);

                File.Delete(zipPath);

                if (IsAdbAvailable())
                {
                    OnStatusChanged?.Invoke("ADB repaired successfully", true);
                    return true;
                }

                OnStatusChanged?.Invoke("Repair finished but adb.exe still missing", false);
                return false;
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke("ADB repair failed: " + ex.Message, false);
                return false;
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
        }

        // Used by the manual "Reconnect" button. A plain Disconnect() +
        // retry is often not enough after a physical USB unplug/replug:
        // the background adb server (port 5037) can be left with a stale
        // view of the device, so `adb forward` silently keeps failing even
        // though `adb devices` would eventually show the phone again. The
        // only reliable fix is to kill the adb server - the next adb
        // command (the forward call in ConnectAsync) automatically starts
        // a fresh one and re-detects the device immediately.
        public void ForceReconnect()
        {
            ExecuteAdbCommand("kill-server");
            Disconnect();
        }
    }
}