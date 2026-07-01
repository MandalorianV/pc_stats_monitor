namespace Monitor.Core.Models
{
    public class PcStatsDto
    {
        public float CpuLoad { get; set; }
        public float CpuTemp { get; set; }
        public float CpuClock { get; set; }
        public float CpuWatt { get; set; }
        public float GpuLoad { get; set; }
        public float GpuTemp { get; set; }
        public float GpuClock { get; set; }
        public float GpuWatt { get; set; }
        public float GpuVramUsedMb { get; set; }
        public float RamUsage { get; set; }
        public float TotalWatt { get; set; }
        public float RamUsedGb { get; set; }
        public float RamTotalGb { get; set; }
    }
}