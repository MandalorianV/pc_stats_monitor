namespace Monitor.Core.Models
{
    public class PcStatsDto
    {
        public float CpuLoad { get; set; }
        public float CpuTemp { get; set; }
        public float CpuClock { get; set; }
        public float CpuWatt { get; set; }
        public float CpuVcore { get; set; }
        
        public float GpuLoad { get; set; }
        public float GpuTemp { get; set; }
        public float GpuJunctionTemp { get; set; }
        public float GpuHotspotTemp { get; set; }
        public float GpuClock { get; set; }
        
        // --- GPU Güç Metrikleri ---
        public float GpuWatt { get; set; }           // GPU Power / Package Power
        public float GpuCorePower { get; set; }      // GPU Core Power
        public float GpuBoardPower { get; set; }     // GPU Board Power (SensorType.Load/Power)

        public float GpuVramUsedMb { get; set; }

        // --- GPU Fan Yüzdeleri (%) ---
        public float GpuFan1Percentage { get; set; }
        public float GpuFan2Percentage { get; set; }

        // --- GPU Fan Devirleri (RPM) ---
        public float GpuFan1Rpm { get; set; }
        public float GpuFan2Rpm { get; set; }

        public float RamUsage { get; set; }
        public float TotalWatt { get; set; }
        public float RamUsedGb { get; set; }
        public float RamTotalGb { get; set; }
        public float CpuFanRpm { get; set; }
        public float CpuOptFanRpm { get; set; }
        public float CpuPumpRpm { get; set; }
        public float VrmMosTemp { get; set; }
        public float VrmSocTemp { get; set; }
        public string ScreenColor { get; set; } = "#121212";
    }
}