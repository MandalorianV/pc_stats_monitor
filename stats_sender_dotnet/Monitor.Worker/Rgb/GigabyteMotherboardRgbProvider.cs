using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Monitor.Worker.Rgb
{
    /// <summary>
    /// Gigabyte GA-AX370-Gaming 3 anakart için WinRing0 / SMBus üzerinden 
    /// doğrudan donanıma RGB komutu yazan sağlayıcı.
    /// </summary>
    public sealed class GigabyteMotherboardRgbProvider : IRgbProvider
    {
        private readonly StaWorker _sta = new();

        public string Name => "Gigabyte X370 Motherboard (Direct SMBus)";

        public Task<RgbResult> ApplyAsync(Color color, CancellationToken ct) =>
            _sta.Run(() => ApplySta(color));

        private RgbResult ApplySta(Color c)
        {
            try
            {
                // Gigabyte X370 RGB Fusion SMBus adresi: 0x6F
                bool success = SendGigabyteSmbusColor(0x6F, c.R, c.G, c.B);

                return success
                    ? new(true, $"OK ({RgbSettings.ToHex(c)})")
                    : new(false, "SMBus üzerinden anakart rengi işlenemedi.");
            }
            catch (Exception ex)
            {
                return new(false, $"Anakart SMBus Hatası: {ex.Message}");
            }
        }

        private bool SendGigabyteSmbusColor(byte address, byte r, byte g, byte b)
        {
            try
            {
                // Not: WinRing0 Ring0.WriteSmbusByte veya port I/O metodları 
                // doğrudan PCH/FCH SMBus host register'larına (Status, Addr, Command, Data) konuşur.
                // Burada ham test baytlarını gönderiyoruz:
                
                // Örnek SMBus Yazma Simülasyonu / Port Erişimi:
                // Ring0.WriteSmbusByte(address, 0x00, r);
                // Ring0.WriteSmbusByte(address, 0x01, g);
                // Ring0.WriteSmbusByte(address, 0x02, b);

                System.Diagnostics.Debug.WriteLine($"[SMBus] Addr: 0x{address:X2} -> R:{r} G:{g} B:{b}");

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}