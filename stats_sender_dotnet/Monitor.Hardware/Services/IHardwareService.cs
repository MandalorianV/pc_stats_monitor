using Monitor.Core.Models;

namespace Monitor.Hardware.Services
{
    public interface IHardwareService
    {
        void Initialize();
        void Shutdown();
        PcStatsDto RefreshAndRead(bool updateMotherboard);
    }
}