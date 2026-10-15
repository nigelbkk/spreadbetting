using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace SpreadTrader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private DispatcherTimer _heartbeatTimer;
        private const string HeartbeatPath = @"C:\Temp\SpreadTrader\heartbeat.json";

        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            Directory.CreateDirectory(@"C:\Temp");
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            StartHeartbeat();
            Exit += (_, __) => WriteHeartbeat("stopped");
        }

        private void StartHeartbeat()
        {
            _heartbeatTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(15)
            };
            _heartbeatTimer.Tick += (_, __) => WriteHeartbeat("running");
            _heartbeatTimer.Start();
            WriteHeartbeat("running");
        }

        private static void WriteHeartbeat(string state)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HeartbeatPath));
                string json = $"{{\"utc\":\"{DateTime.UtcNow:O}\",\"pid\":{Process.GetCurrentProcess().Id},\"state\":\"{state}\"}}";
                File.WriteAllText(HeartbeatPath, json + Environment.NewLine);
            }
            catch
            {
                // Diagnostics must never interfere with the trading UI.
            }
        }

        private static void LogFatal(string source, Exception ex)
        {
            try
            {
                Trace.WriteLine($"[{DateTime.UtcNow:O}] {source}: {ex}");
                File.AppendAllText(@"C:\Temp\spreadtrader-fatal.log", $"[{DateTime.UtcNow:O}] {source}: {ex}{Environment.NewLine}");
            }
            catch
            {
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogFatal("DispatcherUnhandledException", e.Exception);
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogFatal("UnhandledException", e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogFatal("UnobservedTaskException", e.Exception);
            e.SetObserved();
        }
    }
}
