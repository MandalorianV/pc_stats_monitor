using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Monitor.Hardware.Services;
using Monitor.Network.Services;
using Monitor.Worker.Rgb;

namespace Monitor.Worker
{
    public class Worker : BackgroundService
    {
        private readonly IHardwareService _hardwareService;
        private readonly ITcpStreamerService _tcpService;
        private readonly MainForm _mainForm;
        private readonly SerialFanSender _serialFanSender;
        private readonly RgbService _rgb;

        private const string PhoneIp = "127.0.0.1";
        private const int PhonePort = 5000;

        public Worker(
            IHardwareService hardwareService, 
            ITcpStreamerService tcpService, 
            SerialFanSender serialFanSender, 
            MainForm mainForm,
            RgbService rgb)
        {
            _hardwareService = hardwareService;
            _tcpService = tcpService;
            _serialFanSender = serialFanSender;
            _mainForm = mainForm;
            _rgb = rgb;
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

                // Android uygulaması için seçilen ekran rengi Hex string olarak atanıyor
                stats.ScreenColor = _rgb.GetColorHex(RgbService.Target.Screen);

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

                    // Stay in this loop only while the connection is actually alive.
                    while (!token.IsCancellationRequested && _tcpService.IsConnected)
                    {
                        await Task.Delay(1000, token);
                    }
                }
                catch
                {
                    consecutiveFailures++;

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