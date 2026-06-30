using System;
using System.Threading.Tasks;
using Monitor.Core.Models;

namespace Monitor.Network.Services
{
    public interface ITcpStreamerService
    {
        event Action<string, bool> OnStatusChanged; // status text + success flag, for the UI
        bool IsConnected { get; }
        bool IsAdbAvailable();
        Task<bool> RepairAdbAsync();
        Task ConnectAsync(string ip, int port);
        Task SendStatsAsync(PcStatsDto stats);
        void TriggerAdbControl(bool wakeUp);
        void Disconnect();
    }
}