using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HidSharp;

namespace Monitor.Worker.Rgb
{
    /// <summary>
    /// Corsair K68 RGB -> doğrudan USB HID (iCUE gerekmez).
    /// Protokol: açık kaynak OpenRGB/ckb-next çalışmalarındaki "Corsair legacy peripheral" protokolü.
    /// Her mesaj 64 baytlık bir pakettir (başına report-id 0 eklenir).
    /// </summary>
    public sealed class CorsairHidProvider : IRgbProvider
    {
        private const int VendorId = 0x1B1C;
        private const int ProductId = 0x1B4F;   // K68 RGB

        private const byte CmdWrite = 0x07;
        private const byte CmdStream = 0x7F;
        private const byte PropSpecialFunction = 0x04;
        private const byte PropLightingControl = 0x05;
        private const byte PropKeyMapping = 0x40;
        private const byte PropSubmitColor24 = 0x28;
        private const byte ModeSoftware = 0x02;

        public string Name => "Corsair K68 RGB (HID)";

        public Task<RgbResult> ApplyAsync(Color color, CancellationToken ct) =>
            Task.Run(() => Apply(color), ct);

        private static RgbResult Apply(Color color)
        {
            var devices = DeviceList.Local.GetHidDevices(VendorId, ProductId).ToList();
            if (devices.Count == 0)
                return new(false, "K68 RGB bulunamadı (USB'ye takılı mı?)");

            int variant = RgbSettings.Load().CorsairVariant;
            string? lastError = null;

            // OpenRGB bu klavyede kontrol arayüzü olarak interface 1'i kullanıyor (yolda "mi_01").
            foreach (var dev in devices.OrderByDescending(d =>
                         d.DevicePath.Contains("mi_01", StringComparison.OrdinalIgnoreCase)))
            {
                int featureLen, outputLen;
                try
                {
                    featureLen = dev.GetMaxFeatureReportLength();
                    outputLen = dev.GetMaxOutputReportLength();
                }
                catch (Exception ex) { lastError = ex.Message; continue; }

                bool useOutput = variant == 4;
                if (useOutput ? outputLen < 65 : featureLen < 65) continue;   // kontrol arayüzü değil

                try
                {
                    if (!dev.TryOpen(out HidStream stream))
                    {
                        lastError = "Klavye açılamadı (iCUE gibi başka bir program kullanıyor olabilir)";
                        continue;
                    }

                    using (stream)
                    {
                        Action<byte[]> send = pkt64 =>
                        {
                            int len = Math.Max(useOutput ? outputLen : featureLen, 65);
                            var buf = new byte[len];
                            buf[0] = 0x00;                       // report id
                            Array.Copy(pkt64, 0, buf, 1, 64);
                            if (useOutput) stream.Write(buf); else stream.SetFeature(buf);
                        };
                        SendAll(send, color, variant);
                    }
                    return new(true, $"OK v{variant} ({RgbSettings.ToHex(color)})");
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                }
            }

            return new(false, lastError ?? "Uygun HID arayüzü bulunamadı");
        }

        private static void SendAll(Action<byte[]> send, Color c, int variant)
        {
            // Varyant 2, 3, 4: aydınlatmanın kabul edilmesi için "özel fonksiyon" yazılım modu gerekiyor.
            if (variant >= 2)
                send(Pkt(CmdWrite, PropSpecialFunction, ModeSoftware));

            send(Pkt(CmdWrite, PropLightingControl, ModeSoftware, 0x00, 0x03));   // 3 = klavye

            // Varyant 3, 4: K95 Platinum'da tuş girişini yazılım modunda canlı tutan tuş eşleme paketleri
            if (variant >= 3)
            {
                send(Pkt(CmdWrite, PropLightingControl, 0x08, 0x00, 0x01));
                foreach (var p in KeyMappingPackets())
                    send(p);
            }

            // Kırmızı, yeşil, mavi düzlemlerini (144 bayt) sırayla gönder
            byte[][] planes = { Plane(c.R), Plane(c.G), Plane(c.B) };
            byte[] finish = { 1, 1, 2 };

            for (int ch = 0; ch < 3; ch++)
            {
                send(StreamPkt(1, planes[ch], 0, 60));
                send(StreamPkt(2, planes[ch], 60, 60));
                send(StreamPkt(3, planes[ch], 120, 24));
                send(Pkt(CmdWrite, PropSubmitColor24, (byte)(ch + 1), 0x03, finish[ch]));
            }
        }

        private static byte[][] KeyMappingPackets()
        {
            var skip = new HashSet<int> { 0x31, 0x41, 0x42, 0x48, 0x49, 0x51, 0x55, 0x6F, 0x7E, 0x7F, 0x80, 0x81 };
            var packets = new byte[4][];
            int id = 0;
            for (int i = 0; i < 4; i++)
            {
                var p = new byte[64];
                p[0] = CmdWrite;
                p[1] = PropKeyMapping;
                p[2] = 0x1E;
                for (int j = 0; j < 30; j++)
                {
                    while (skip.Contains(id)) id++;
                    p[4 + 2 * j] = (byte)id++;
                    p[5 + 2 * j] = 0xC0;
                }
                packets[i] = p;
            }
            return packets;
        }

        private static byte[] Plane(byte value)
        {
            var p = new byte[144];
            Array.Fill(p, value);      // tüm tuşlar aynı renk
            return p;
        }

        private static byte[] StreamPkt(byte packetId, byte[] data, int offset, int size)
        {
            var pkt = new byte[64];
            pkt[0] = CmdStream;
            pkt[1] = packetId;
            pkt[2] = (byte)size;
            Array.Copy(data, offset, pkt, 4, size);
            return pkt;
        }

        private static byte[] Pkt(params byte[] bytes)
        {
            var pkt = new byte[64];
            Array.Copy(bytes, pkt, bytes.Length);
            return pkt;
        }
    }
}