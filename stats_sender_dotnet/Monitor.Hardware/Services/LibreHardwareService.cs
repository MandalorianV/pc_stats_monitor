using System;
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
                // =========================================================
                // 1. CPU SENSÖRLERİ
                // =========================================================
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
                    if (clockSensors.Any()) p.CpuClock = clockSensors.Max(s => s.Value ?? 0);

                    var cpuPowerSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power && s.Name.Contains("Package"))
                                         ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power);
                    p.CpuWatt = cpuPowerSensor?.Value ?? 0;

                    var vcoreSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Voltage && s.Name.Contains("VCore"))
                                      ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Voltage && s.Name.Contains("Core") && !s.Name.Contains("SOC"))
                                      ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Voltage);
                    p.CpuVcore = vcoreSensor?.Value ?? 0;
                }

                // =========================================================
                // 2. GPU SENSÖRLERİ (Nvidia, AMD, Intel)
                // =========================================================
                if (hw.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel)
                {
                    var gpuLoadSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && (s.Name.Contains("Core") || s.Name.Contains("Graphics")))
                                        ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
                    p.GpuLoad = gpuLoadSensor?.Value ?? 0;

                    // GPU Core Sıcaklığı
                    var gpuTempSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && (s.Name.Equals("GPU Core", StringComparison.OrdinalIgnoreCase) || s.Name.Equals("GPU Temperature", StringComparison.OrdinalIgnoreCase)))
                                        ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                    p.GpuTemp = gpuTempSensor?.Value ?? 0;

                    // GPU Hotspot Sıcaklığı
                    var gpuHotspotSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Equals("GPU Hot Spot", StringComparison.OrdinalIgnoreCase));
                    p.GpuHotspotTemp = gpuHotspotSensor?.Value ?? 0;

                    // GPU Memory Junction Sıcaklığı
                    var gpuJunctionSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && (s.Name.Equals("GPU Memory Junction", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Junction")));
                    p.GpuJunctionTemp = gpuJunctionSensor?.Value ?? 0;

                    // GPU Fan Yüzdeleri (%) (SensorType.Control)
                    var gpuFan1Control = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Control && s.Name.Equals("GPU Fan 1", StringComparison.OrdinalIgnoreCase));
                    var gpuFan2Control = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Control && s.Name.Equals("GPU Fan 2", StringComparison.OrdinalIgnoreCase));
                    p.GpuFan1Percentage = gpuFan1Control?.Value ?? 0;
                    p.GpuFan2Percentage = gpuFan2Control?.Value ?? 0;

                    // GPU Fan Devirleri (RPM) (SensorType.Fan)
                    var gpuFan1Rpm = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Fan && s.Name.Equals("GPU Fan 1", StringComparison.OrdinalIgnoreCase));
                    var gpuFan2Rpm = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Fan && s.Name.Equals("GPU Fan 2", StringComparison.OrdinalIgnoreCase));
                    p.GpuFan1Rpm = gpuFan1Rpm?.Value ?? 0;
                    p.GpuFan2Rpm = gpuFan2Rpm?.Value ?? 0;

                    var gpuClockSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Clock && (s.Name.Contains("Core") || s.Name.Contains("Graphics")));
                    p.GpuClock = gpuClockSensor?.Value ?? 0;

                    // GPU Power (Genel / Package)
                    var gpuPowerSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power && (s.Name.Equals("GPU Power", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Package")))
                                         ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power);
                    p.GpuWatt = gpuPowerSensor?.Value ?? 0;

                    // GPU Core Power
                    var gpuCorePowerSensor = hw.Sensors.FirstOrDefault(s => (s.SensorType == SensorType.Load || s.SensorType == SensorType.Load) 
                                                                         && s.Name.Equals("GPU Power", StringComparison.OrdinalIgnoreCase));
                    p.GpuCorePower = gpuCorePowerSensor?.Value ?? 0;

                    // GPU Board Power (SensorType.Load & Power Arama)
                    var gpuBoardPowerSensor = hw.Sensors.FirstOrDefault(s => (s.SensorType == SensorType.Load || s.SensorType == SensorType.Power) 
                                                                         && (s.Name.Equals("GPU Board Power", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Board Power")));
                    p.GpuBoardPower = gpuBoardPowerSensor?.Value ?? 0;

                    var vramSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.SmallData && s.Name == "GPU Memory Used")
                                     ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.SmallData && s.Name.Contains("Memory Used") && !s.Name.Contains("Shared"));
                    p.GpuVramUsedMb = vramSensor?.Value ?? 0;
                }

                // =========================================================
                // 3. RAM SENSÖRLERİ
                // =========================================================
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

                // =========================================================
                // 4. ANAKART SENSÖRLERİ (GA-AX370-Gaming 3)
                // =========================================================
                if (hw.HardwareType == HardwareType.Motherboard)
                {
                    ISensor? cpuFanSensor = null;
                    ISensor? cpuOptFanSensor = null;
                    ISensor? cpuPumpSensor = null;
                    ISensor? vrmMosTemp = null;
                    ISensor? vrmSocTemp = null;

                    foreach (var subHw in hw.SubHardware)
                    {
                        var totalPowerSensor = subHw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power && (s.Name.Contains("Total") || s.Name.Contains("System")));
                        if (totalPowerSensor != null && totalPowerSensor.Value.HasValue)
                        {
                            motherboardTotalPower = totalPowerSensor.Value.Value;
                            motherboardPowerFound = true;
                        }

                        cpuFanSensor ??= subHw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Fan && s.Name == "Fan #1");
                        cpuOptFanSensor ??= subHw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Fan && s.Name == "Fan #5");
                        cpuPumpSensor ??= subHw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Fan && s.Name == "Fan #4");
                        vrmMosTemp ??= subHw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name == "Temperature #5");
                        vrmSocTemp ??= subHw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name == "Temperature #6");
                    }

                    if (cpuFanSensor != null) p.CpuFanRpm = cpuFanSensor.Value ?? 0;
                    if (cpuOptFanSensor != null) p.CpuOptFanRpm = cpuOptFanSensor.Value ?? 0;
                    if (cpuPumpSensor != null) p.CpuPumpRpm = cpuPumpSensor.Value ?? 0;
                    if (vrmMosTemp != null) p.VrmMosTemp = vrmMosTemp.Value ?? 0;
                    if (vrmSocTemp != null) p.VrmSocTemp = vrmSocTemp.Value ?? 0;
                }
            }

            // Orijinal Total Power Mantığına Dokunulmadı
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