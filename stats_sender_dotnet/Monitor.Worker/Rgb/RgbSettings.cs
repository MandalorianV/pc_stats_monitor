using System;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace Monitor.Worker.Rgb
{
    public sealed class RgbSettings
    {
        public string Keyboard { get; set; } = "#FF0000";
        public string Screen   { get; set; } = "#121212"; // Yeni Ekran Rengi Ayarı

        public int CorsairVariant { get; set; } = 3;

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PCStatsSender", "rgb.json");

        public static RgbSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<RgbSettings>(File.ReadAllText(FilePath)) ?? new RgbSettings();
            }
            catch { }
            return new RgbSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath,
                    JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public static Color Parse(string? hex)
        {
            try { return ColorTranslator.FromHtml(hex ?? "#FF0000"); }
            catch { return Color.Red; }
        }

        public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}