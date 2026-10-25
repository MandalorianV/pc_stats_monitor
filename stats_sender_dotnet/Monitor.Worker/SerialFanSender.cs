using System;
using System.Globalization;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace Monitor.Worker
{
    // PC Stats agent'in her tick'te okudugu ayni GpuTemp degerini
    // Arduino'ya seri port uzerinden yollar. Telefona giden TCP akisina
    // dokunmaz, paralel calisir.
    //
    // Port artik hardcoded degil: her (yeniden) baglanma denemesinde Windows'a
    // "hangi COM portu Arduino/CH340/USB-Serial gibi gorunuyor" diye soruyor.
    // Boylece format/surucu guncelleme/farkli USB girisi sonrasi COM numarasi
    // degisse bile elle duzeltmeye gerek kalmiyor.
    public class SerialFanSender
    {
        private SerialPort? _port;
        private readonly string? _preferredPort; // opsiyonel: "COM5" gibi sabit bir tercih verilirse once o denenir

        // Arduino portu her acildiginda bootloader resetleniyor ve birkac
        // yuz ms boyunca veri kabul etmiyor; bu sure icinde yazma denemesi
        // sessizce kayboluyor, o yuzden kisa bir isinma suresi bekliyoruz.
        private DateTime _openedAt;
        private static readonly TimeSpan WarmupDelay = TimeSpan.FromSeconds(2);

        // UI'nin durumu gosterebilmesi icin: "Baglandi (COM5)" / "Bulunamadi" vb.
        public event Action<string, bool>? OnStatusChanged;
        public bool IsConnected => _port != null && _port.IsOpen;

        public SerialFanSender(string? preferredPort = null)
        {
            _preferredPort = preferredPort;
        }

        private void EnsureOpen()
        {
            if (_port != null && _port.IsOpen) return;

            var candidate = FindArduinoPort() ?? _preferredPort;
            if (candidate == null)
            {
                OnStatusChanged?.Invoke("Bos COM portu bulunamadi", false);
                return; // bir sonraki tick'te tekrar dener
            }

            try
            {
                _port = new SerialPort(candidate, 9600)
                {
                    NewLine = "\n",
                    WriteTimeout = 1000,
                    ReadTimeout = 1000,
                };
                _port.Open();
                _openedAt = DateTime.UtcNow;
                OnStatusChanged?.Invoke($"Baglandi ({candidate})", true);
            }
            catch (Exception ex)
            {
                _port = null; // Arduino takili degilse veya port mesgulse sessizce vazgec
                OnStatusChanged?.Invoke("Baglanamadi: " + ex.Message, false);
            }
        }

        public void SendGpuTemp(float gpuTemp)
        {
            EnsureOpen();
            if (_port == null || !_port.IsOpen) return;
            if (DateTime.UtcNow - _openedAt < WarmupDelay) return; // bootloader isinma suresi

            try
            {
                _port.WriteLine(gpuTemp.ToString("F1", CultureInfo.InvariantCulture)); // ornek: "41.3"
            }
            catch
            {
                try { _port.Close(); } catch { }
                _port = null; // koptuysa bir sonraki tick'te yeniden acmayi ve portu yeniden bulmayi dener
                OnStatusChanged?.Invoke("Baglanti koptu, yeniden denenecek...", false);
            }
        }

        // Windows'un cihaz adi listesinde Arduino/CH340/USB-Serial gibi
        // gecen bir COM portu arar (genuine Uno/Nano "Arduino ...", klon
        // kartlar genelde "USB-SERIAL CH340" ya da "USB Serial Device"
        // olarak gorunur). Bulamazsa null doner, cagiran taraf preferredPort'a
        // duser.
        private static string? FindArduinoPort()
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT Name FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");

                foreach (var device in searcher.Get())
                {
                    var name = device["Name"]?.ToString() ?? "";
                    bool looksLikeArduino =
                        name.Contains("Arduino", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("CH340", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("USB-SERIAL", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("USB Serial Device", StringComparison.OrdinalIgnoreCase);

                    if (!looksLikeArduino) continue;

                    var match = Regex.Match(name, @"COM\d+");
                    if (match.Success) return match.Value;
                }
            }
            catch
            {
                // WMI bazi ortamlarda kullanilamayabilir - sessizce vazgec,
                // cagiran taraf preferredPort fallback'ine duser.
            }

            return null;
        }
    }
}
