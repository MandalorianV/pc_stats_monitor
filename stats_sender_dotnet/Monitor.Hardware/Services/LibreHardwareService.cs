using System.Linq;
using LibreHardwareMonitor.Hardware;
using Monitor.Core.Models;

namespace Monitor.Hardware.Services
{
    public class LibreHardwareService : IHardwareService
    {
        private Computer? _computer;

        public void Initialize()
        {
            _computer = new Computer 
            { 
                IsCpuEnabled = true, 
                IsGpuEnabled = true, 
                IsMemoryEnabled = true, 
                IsMotherboardEnabled = true 
            };
            _computer.Open();
        }

        public void Shutdown()
        {
            _computer?.Close();
        }

        public PcStatsDto RefreshAndRead(bool updateMotherboard)
        {
            if (_computer == null) return new PcStatsDto();

            // Skip motherboard refresh on most ticks: cheaper polling, only
            // refreshed once every 5 ticks (it's only used for TotalWatt).
            foreach (var hw in _computer.Hardware)
            {
                if (hw.HardwareType == HardwareType.Motherboard && !updateMotherboard)
                    continue;

                hw.Update();
                foreach (var sub in hw.SubHardware)
                    sub.Update();
            }

            var p = new PcStatsDto();
            float motherboardTotalPower = 0;
            bool motherboardPowerFound = false;

            foreach (var hw in _computer.Hardware)
            {
                if (hw.HardwareType == HardwareType.Cpu)
                {
                    var loadSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Total"))
                                     ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
                    p.CpuLoad = loadSensor?.Value ?? 0;

                    var tempSensors = hw.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue).ToList();
                    if (tempSensors.Any())
                    {
                        var targetTemp = tempSensors.FirstOrDefault(s => s.Name.Contains("Package"))
                                         ?? tempSensors.FirstOrDefault(s => s.Name.Contains("Tctl"))
                                         ?? tempSensors.OrderByDescending(s => s.Value).First();
                        p.CpuTemp = targetTemp.Value ?? 0;
                    }

                    var clockSensors = hw.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Name.Contains("Core")).ToList();
                    if (clockSensors.Any()) p.CpuClock = clockSensors.Average(s => s.Value ?? 0);

                    var cpuPowerSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power && s.Name.Contains("Package"))
                                         ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power);
                    p.CpuWatt = cpuPowerSensor?.Value ?? 0;
                }

                if (hw.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel)
                {
                    var gpuLoadSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && (s.Name.Contains("Core") || s.Name.Contains("Graphics")))
                                        ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
                    p.GpuLoad = gpuLoadSensor?.Value ?? 0;

                    var gpuTempSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Core"))
                                        ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                    p.GpuTemp = gpuTempSensor?.Value ?? 0;

                    var gpuClockSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Clock && (s.Name.Contains("Core") || s.Name.Contains("Graphics")));
                    p.GpuClock = gpuClockSensor?.Value ?? 0;

                    var gpuPowerSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power && (s.Name.Contains("Package") || s.Name.Contains("Total")))
                                         ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power);
                    p.GpuWatt = gpuPowerSensor?.Value ?? 0;

                    var vramSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.SmallData && s.Name == "GPU Memory Used")
                                     ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.SmallData && s.Name.Contains("Memory Used") && !s.Name.Contains("Shared"));
                    p.GpuVramUsedMb = vramSensor?.Value ?? 0;
                }

                if (hw.HardwareType == HardwareType.Memory)
                {
                    var ramLoadSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "Memory")
                                        ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Memory") && !s.Name.Contains("Virtual"));
                    p.RamUsage = ramLoadSensor?.Value ?? 0;

                    var ramUsedSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Used");
                    var ramAvailableSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Available");
                    p.RamUsedGb = ramUsedSensor?.Value ?? 0;
                    p.RamTotalGb = p.RamUsedGb + (ramAvailableSensor?.Value ?? 0);
                }

                if (hw.HardwareType == HardwareType.Motherboard)
                {
                    foreach (var subHw in hw.SubHardware)
                    {
                        var totalPowerSensor = subHw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power && (s.Name.Contains("Total") || s.Name.Contains("System")));
                        if (totalPowerSensor != null && totalPowerSensor.Value.HasValue)
                        {
                            motherboardTotalPower = totalPowerSensor.Value.Value;
                            motherboardPowerFound = true;
                        }
                    }
                }
            }

            if (motherboardPowerFound)
            {
                p.TotalWatt = motherboardTotalPower;
            }
            else
            {
                float baseOffset = (p.CpuLoad > 10 || p.GpuLoad > 10) ? 55f : 40f;
                p.TotalWatt = p.CpuWatt + p.GpuWatt + baseOffset;
            }

            return p;
        }
    }
}