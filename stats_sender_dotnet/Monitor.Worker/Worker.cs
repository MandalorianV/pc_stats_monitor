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

        public Worker(IHardwareService hardwareService, ITcpStreamerService tcpService, MainForm mainForm)
        {
            _hardwareService = hardwareService;
            _tcpService = tcpService;
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

                // Poll/send interval
                await Task.Delay(800, stoppingToken);
            }
        }

        private async Task NetworkConnectLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _tcpService.ConnectAsync(PhoneIp, PhonePort);

                    // Keep this loop idle while the connection stays alive
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(5000, token);
                    }
                }
                catch
                {
                    // Connection failed, retry after a short delay
                    await Task.Delay(3000, token);
                }
            }
        }
    }
}