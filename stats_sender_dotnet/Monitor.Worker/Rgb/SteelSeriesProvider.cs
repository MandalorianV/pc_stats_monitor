using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Monitor.Worker.Rgb
{
    /// <summary>
    /// SteelSeries Rival 3 Wireless -> GameSense SDK (yerel HTTP). SteelSeries GG açık olmalı.
    /// GameSense etkinlikleri ~60 sn sonra zaman aşımına uğradığı için servis bunu periyodik yeniler.
    /// </summary>
    public sealed class SteelSeriesProvider : IRgbProvider
    {
        private const string Game = "PCSTATSSENDER";
        private const string EventName = "STATIC_COLOR";
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(3) };

        private string? _boundAddress;
        private int? _boundArgb;

        public string Name => "SteelSeries Rival 3 Wireless";

        private static string? ReadCoreAddress()
        {
            var pd = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string[] candidates =
            {
                Path.Combine(pd, "SteelSeries", "SteelSeries Engine 3", "coreProps.json"),
                Path.Combine(pd, "SteelSeries", "GG", "coreProps.json"),
            };
            foreach (var path in candidates)
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    using var doc = JsonDocument.Parse(File.ReadAllText(path));
                    if (doc.RootElement.TryGetProperty("address", out var a) && a.GetString() is { Length: > 0 } s)
                        return s;
                }
                catch { }
            }
            return null;
        }

        public async Task<RgbResult> ApplyAsync(Color color, CancellationToken ct)
        {
            var address = ReadCoreAddress();
            if (address == null) return new(false, "SteelSeries GG bulunamadı (açık mı?)");
            var baseUrl = "http://" + address;

            try
            {
                if (_boundAddress != address || _boundArgb != color.ToArgb())
                {
                    var (ok, text) = await PostAsync(baseUrl, "/game_metadata", new Dictionary<string, object>
                    {
                        ["game"] = Game,
                        ["game_display_name"] = "PC Stats Sender",
                        ["developer"] = "Local",
                        ["deinitialize_timer_length_ms"] = 60000,
                    }, ct);
                    if (!ok) return new(false, "game_metadata: " + text);

                    (ok, text) = await PostAsync(baseUrl, "/bind_game_event", BuildBind(color), ct);
                    if (!ok) return new(false, "bind_game_event: " + text);

                    _boundAddress = address;
                    _boundArgb = color.ToArgb();
                }

                var (ok2, text2) = await PostAsync(baseUrl, "/game_event", new Dictionary<string, object>
                {
                    ["game"] = Game,
                    ["event"] = EventName,
                    ["data"] = new Dictionary<string, object> { ["value"] = 1 },
                }, ct);
                return ok2 ? new(true, $"OK ({RgbSettings.ToHex(color)})") : new(false, "game_event: " + text2);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _boundAddress = null;
                return new(false, ex.Message);
            }
        }

        private static Dictionary<string, object> BuildBind(Color c)
        {
            Dictionary<string, object> Handler(string zone) => new()
            {
                ["device-type"] = "mouse",
                ["zone"] = zone,
                ["mode"] = "color",
                ["color"] = new Dictionary<string, object>
                {
                    ["red"] = (int)c.R, ["green"] = (int)c.G, ["blue"] = (int)c.B,
                },
            };

            return new Dictionary<string, object>
            {
                ["game"] = Game,
                ["event"] = EventName,
                ["min_value"] = 0,
                ["max_value"] = 1,
                ["icon_id"] = 0,
                ["value_optional"] = true,
                ["handlers"] = new object[] { Handler("wheel"), Handler("logo") },
            };
        }

        private static async Task<(bool ok, string text)> PostAsync(string baseUrl, string path, object body, CancellationToken ct)
        {
            using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var resp = await Http.PostAsync(baseUrl + path, content, ct);
            var text = await resp.Content.ReadAsStringAsync(ct);
            return (resp.IsSuccessStatusCode, resp.IsSuccessStatusCode ? "OK" : $"{(int)resp.StatusCode} {text}");
        }
    }
}
