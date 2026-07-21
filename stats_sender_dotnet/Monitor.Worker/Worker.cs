using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Monitor.Hardware.Services;
using Monitor.Network.Services;

namespace Monitor.Worker
{
    public class Worker : BackgroundService
    {
        private readonly IHardwareService _hardwareService;
        private readonly ITcpStreamerService _tcpService;
        private readonly MainForm _mainForm;

        private const string PhoneIp = "127.0.0.1";
        private const int PhonePort = 5000;
        private readonly SerialFanSender _serialFanSender;

        public Worker(IHardwareService hardwareService, ITcpStreamerService tcpService, SerialFanSender serialFanSender, MainForm mainForm)
        {
            _hardwareService = hardwareService;
            _tcpService = tcpService;
            _serialFanSender = serialFanSender;
            _mainForm = mainForm;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _hardwareService.Initialize();

            // Run the TCP connection loop in parallel
            _ = Task.Run(() => NetworkConnectLoop(stoppingToken), stoppingToken);

            int tick = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                tick++;
                bool updateMotherboard = (tick % 5 == 0);

                // Read hardware sensors
                var stats = _hardwareService.RefreshAndRead(updateMotherboard);

                // Update the form UI
                _mainForm.UpdateUiValues(stats);

                // Send over TCP
                await _tcpService.SendStatsAsync(stats);
                _serialFanSender.SendGpuTemp(stats.GpuTemp);
                // Poll/send interval
                await Task.Delay(800, stoppingToken);
            }
        }

        private async Task NetworkConnectLoop(CancellationToken token)
        {
            int consecutiveFailures = 0;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _tcpService.ConnectAsync(PhoneIp, PhonePort);
                    consecutiveFailures = 0;

                    // Stay in this loop only while the connection is actually
                    // alive. As soon as IsConnected goes false (dropped cable,
                    // manual reconnect request, phone restart, etc.) we fall
                    // through and immediately try to reconnect.
                    while (!token.IsCancellationRequested && _tcpService.IsConnected)
                    {
                        await Task.Delay(1000, token);
                    }
                }
                catch
                {
                    consecutiveFailures++;

                    // After a handful of failed attempts (e.g. following a
                    // physical USB unplug/replug) the local adb server can
                    // be left with a stale view of the device, silently
                    // breaking `adb forward` forever. Restarting it here
                    // self-heals this without requiring the user to click
                    // the manual Reconnect button.
                    if (consecutiveFailures >= 5)
                    {
                        _tcpService.ForceReconnect();
                        consecutiveFailures = 0;
                    }

                    // Retry after a short delay
                    await Task.Delay(3000, token);
                }
            }
        }
    }
}