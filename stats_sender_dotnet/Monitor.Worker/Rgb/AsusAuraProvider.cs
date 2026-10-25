using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Monitor.Worker.Rgb
{
    /// <summary>
    /// ASUS TUF ekran kartı -> ASUS Aura SDK (COM, late-binding: "aura.sdk.1").
    /// Armoury Crate / Aura servisi kurulu ve çalışıyor olmalı.
    /// </summary>
    public sealed class AsusAuraProvider : IRgbProvider
    {
        private readonly StaWorker _sta = new();
        private dynamic? _sdk;

        public string Name => "ASUS TUF GPU";

        public Task<RgbResult> ApplyAsync(Color color, CancellationToken ct) =>
            _sta.Run(() => ApplySta(color));

        private RgbResult ApplySta(Color c)
        {
            try
            {
                if (_sdk == null)
                {
                    var type = Type.GetTypeFromProgID("aura.sdk.1");
                    if (type == null) return new(false, "Aura SDK bulunamadı (Armoury Crate kurulu mu?)");
                    _sdk = Activator.CreateInstance(type);
                    try { _sdk!.SwitchMode(); } catch { } // Yazılım kontrolüne geçiş
                }

                uint bgr = (uint)((c.B << 16) | (c.G << 8) | c.R);   // Aura rengi 0x00BBGGRR bekler
                int applied = 0;

                dynamic devices = _sdk!.Enumerate(0);                 // 0 = tüm cihazlar
                if (devices != null)
                {
                    int deviceCount = Convert.ToInt32(devices.Count);
                    for (int i = 0; i < deviceCount; i++)
                    {
                        dynamic dev = devices[i];
                        if (!IsGpu(dev)) continue;

                        dynamic lights = dev.Lights;
                        if (lights != null)
                        {
                            int lightCount = Convert.ToInt32(lights.Count);
                            for (int j = 0; j < lightCount; j++)
                                lights[j].Color = bgr;
                        }

                        dev.Apply();
                        applied++;
                    }
                }

                return applied > 0
                    ? new(true, $"OK ({RgbSettings.ToHex(c)})")
                    : new(false, "Aura'da ekran kartı bulunamadı");
            }
            catch (Exception ex)
            {
                _sdk = null;   // Bir sonraki denemede yeniden bağlan
                return new(false, ex.Message);
            }
        }

        private static bool IsGpu(dynamic dev)
        {
            try
            {
                uint type = Convert.ToUInt32(dev.Type);
                if (type == 0x00020000u || (type & 0xFFFF0000u) == 0x00020000u || (type & 0xFFFF0000u) == 0x00030000u) return true;   
            }
            catch { }

            try
            {
                string name = Convert.ToString(dev.Name) ?? "";
                return name.Contains("TUF", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("GeForce", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("RTX", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("VGA", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
    }
}