using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Monitor.Worker.Rgb
{
    /// <summary>
    /// Corsair K68 RGB -> iCUE SDK v4 (resmi). Gerekenler:
    ///  - iCUE açık olmalı, Settings'te "Enable SDK" işaretli olmalı
    ///  - iCUESDK.x64_2019.dll exe'nin yanında olmalı (Corsair'in resmi GitHub'ından)
    /// </summary>
    public sealed class CorsairProvider : IRgbProvider
    {
        public string Name => "Corsair K68 RGB";

        private static readonly ManualResetEventSlim ConnectedEvent = new(false);
        private static readonly IcueNative.SessionStateChangedHandler Handler = OnState; // GC'ye kaptırma
        private static bool _connectCalled;

        private static void OnState(IntPtr context, IntPtr eventData)
        {
            if (eventData == IntPtr.Zero) return;
            int state = Marshal.ReadInt32(eventData);      // CorsairSessionState (6 = Connected)
            if (state == 6) ConnectedEvent.Set(); else ConnectedEvent.Reset();
        }

        public Task<RgbResult> ApplyAsync(Color color, CancellationToken ct) =>
            Task.Run(() => Apply(color), ct);

        private static RgbResult Apply(Color color)
        {
            if (!_connectCalled)
            {
                int r = IcueNative.CorsairConnect(Handler, IntPtr.Zero);
                if (r != 0) return new(false, $"CorsairConnect hata kodu: {r}");
                _connectCalled = true;
            }

            if (!ConnectedEvent.Wait(3000))
            {
                // Bir sonraki denemede baştan bağlan
                try { IcueNative.CorsairDisconnect(); } catch { }
                _connectCalled = false;
                return new(false, "iCUE'ye bağlanılamadı (iCUE açık mı? SDK etkin mi?)");
            }

            var filter = new IcueNative.CorsairDeviceFilter { deviceTypeMask = 1 }; // 1 = Keyboard
            var devices = new IcueNative.CorsairDeviceInfo[16];
            int err = IcueNative.CorsairGetDevices(ref filter, devices.Length, devices, out int count);
            if (err != 0) return new(false, $"CorsairGetDevices hata kodu: {err}");
            if (count <= 0) return new(false, "iCUE'de klavye bulunamadı");

            int applied = 0;
            for (int d = 0; d < count; d++)
            {
                string id = ToStr(devices[d].id);

                err = IcueNative.CorsairRequestControl(id, 1); // 1 = ExclusiveLightingControl
                if (err != 0) return new(false, $"CorsairRequestControl hata kodu: {err}");

                var positions = new IcueNative.CorsairLedPosition[512];
                err = IcueNative.CorsairGetLedPositions(id, positions.Length, positions, out int n);
                if (err != 0) return new(false, $"CorsairGetLedPositions hata kodu: {err}");

                var colors = new IcueNative.CorsairLedColor[n];
                for (int i = 0; i < n; i++)
                    colors[i] = new IcueNative.CorsairLedColor { id = positions[i].id, r = color.R, g = color.G, b = color.B, a = 255 };

                err = IcueNative.CorsairSetLedColors(id, n, colors);
                if (err != 0) return new(false, $"CorsairSetLedColors hata kodu: {err}");
                applied++;
            }
            return new(true, $"OK ({RgbSettings.ToHex(color)})");
        }

        private static string ToStr(byte[] raw)
        {
            int z = Array.IndexOf(raw, (byte)0);
            return Encoding.ASCII.GetString(raw, 0, z < 0 ? raw.Length : z);
        }
    }

    internal static class IcueNative
    {
        private const string Dll = "iCUESDK.x64_2019.dll";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SessionStateChangedHandler(IntPtr context, IntPtr eventData);

        [StructLayout(LayoutKind.Sequential)]
        public struct CorsairDeviceFilter { public int deviceTypeMask; }

        [StructLayout(LayoutKind.Sequential)]
        public struct CorsairDeviceInfo
        {
            public int type;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public byte[] id;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public byte[] serial;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public byte[] model;
            public int ledCount;
            public int channelCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CorsairLedPosition { public uint id; public double cx; public double cy; }

        [StructLayout(LayoutKind.Sequential)]
        public struct CorsairLedColor { public uint id; public byte r; public byte g; public byte b; public byte a; }

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CorsairConnect(SessionStateChangedHandler onStateChanged, IntPtr context);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CorsairDisconnect();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CorsairGetDevices(ref CorsairDeviceFilter filter, int sizeMax,
            [Out] CorsairDeviceInfo[] devices, out int size);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int CorsairRequestControl(string deviceId, int accessLevel);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int CorsairGetLedPositions(string deviceId, int sizeMax,
            [Out] CorsairLedPosition[] ledPositions, out int size);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int CorsairSetLedColors(string deviceId, int size,
            [In] CorsairLedColor[] ledColors);
    }
}
