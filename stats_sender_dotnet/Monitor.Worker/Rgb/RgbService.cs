using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Monitor.Worker.Rgb
{
    public sealed class RgbService : BackgroundService
    {
        public enum Target { Keyboard, Screen }

        private readonly RgbSettings _settings = RgbSettings.Load();

        private readonly Dictionary<Target, IRgbProvider> _providers = new()
        {
            [Target.Keyboard] = new CorsairHidProvider(),
            [Target.Screen]   = new UnavailableProvider("Android Screen Color", "Telefona TCP üzerinden yollanır"),
        };

        private readonly Dictionary<Target, SemaphoreSlim> _gates = new()
        {
            [Target.Keyboard] = new SemaphoreSlim(1, 1),
            [Target.Screen]   = new SemaphoreSlim(1, 1),
        };

        private readonly Dictionary<Target, RgbResult> _status = new();

        public event Action<Target, string, bool>? StatusChanged;

        public Color GetColor(Target t)
        {
            lock (_settings)
            {
                return RgbSettings.Parse(t switch
                {
                    Target.Screen => _settings.Screen,
                    _             => _settings.Keyboard,
                });
            }
        }

        public string GetColorHex(Target t)
        {
            lock (_settings)
            {
                return t switch
                {
                    Target.Screen => _settings.Screen,
                    _             => _settings.Keyboard,
                };
            }
        }

        public RgbResult? GetStatus(Target t)
        {
            lock (_status) return _status.TryGetValue(t, out var s) ? s : null;
        }

        public async Task SetColorAsync(Target t, Color color)
        {
            lock (_settings)
            {
                var hex = RgbSettings.ToHex(color);
                switch (t)
                {
                    case Target.Screen:   _settings.Screen   = hex; break;
                    default:              _settings.Keyboard = hex; break;
                }
                _settings.Save();
            }
            await ApplyOneAsync(t, CancellationToken.None);
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var lastOk = new Dictionary<Target, DateTime>();

            while (!ct.IsCancellationRequested)
            {
                foreach (var t in _providers.Keys.ToArray())
                {
                    if (t == Target.Screen) continue;

                    bool due = !lastOk.TryGetValue(t, out var when) || DateTime.UtcNow - when > TimeSpan.FromSeconds(60);
                    if (!due) continue;

                    bool ok;
                    try { ok = await ApplyOneAsync(t, ct); }
                    catch (OperationCanceledException) { return; }

                    if (ok) lastOk[t] = DateTime.UtcNow; else lastOk.Remove(t);
                }

                try { await Task.Delay(3000, ct); }
                catch (OperationCanceledException) { return; }
            }
        }

        private async Task<bool> ApplyOneAsync(Target t, CancellationToken ct)
        {
            var gate = _gates[t];
            await gate.WaitAsync(ct);
            try
            {
                RgbResult r;
                try { r = await _providers[t].ApplyAsync(GetColor(t), ct); }
                catch (OperationCanceledException) { throw; }
                catch (DllNotFoundException) { r = new(false, "iCUESDK.x64_2019.dll bulunamadı"); }
                catch (Exception ex) { r = new(false, ex.Message); }

                Publish(t, r);
                return r.Ok;
            }
            finally { gate.Release(); }
        }

        private void Publish(Target t, RgbResult r)
        {
            bool changed;
            lock (_status)
            {
                changed = !_status.TryGetValue(t, out var old) || old != r;
                _status[t] = r;
            }
            if (changed) StatusChanged?.Invoke(t, r.Message, r.Ok);
        }
    }
}