using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monitor.Hardware.Services;
using Monitor.Network.Services;

namespace Monitor.Worker
{
    internal static class Program
    {
        // Path used to log a fatal crash report to the desktop
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "pcstats_error.log");

        [STAThread]
        static void Main(string[] args)
        {
            // Catch unhandled exceptions on the UI thread
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogError("UnhandledException: " + e.ExceptionObject);

            Application.ThreadException += (s, e) =>
                LogError("ThreadException: " + e.Exception);

            try
            {
                // Windows Forms UI setup
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // .NET Generic Host configuration (dependency injection)
                var host = Host.CreateDefaultBuilder(args)
                    .ConfigureServices((hostContext, services) =>
                    {
                        // 1. Hardware reading service (Hardware layer)
                        services.AddSingleton<IHardwareService, LibreHardwareService>();

                        // 2. TCP streaming and ADB control service (Network layer)
                        services.AddSingleton<ITcpStreamerService, TcpStreamerService>();

                        // 3. Tray/window UI
                        services.AddSingleton<MainForm>();

                        // 4. Background worker that runs the polling/streaming loop
                        services.AddHostedService<Worker>();

                        services.AddSingleton(new SerialFanSender("COM5"));
                    })
                    .Build();

                // Resolve the form from the DI container
                var mainForm = host.Services.GetRequiredService<MainForm>();

                // Start the background host (worker services) without blocking
                host.StartAsync();

                // Run the Windows Forms message loop on the main form
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                LogError("Main crash: " + ex);
            }
        }

        private static void LogError(string message)
        {
            try
            {
                File.AppendAllText(LogPath,
                    DateTime.Now + " - " + message + Environment.NewLine + Environment.NewLine);
            }
            catch { }
        }
    }
}