using System.Windows;
using WinHUD.Services;
using Serilog;
using System.Windows.Threading;

namespace WinHUD
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : global::System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LoggerService.Initialize();

            // Catch all unhandled exceptions
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.Exception, "A fatal unhandled exception occurred in the application!");
            System.Diagnostics.Debug.WriteLine($"[FATAL] {e.Exception.Message}");
            // Prevent default crash dialog, or let it crash depending on preference.
            // For now, let it crash gracefully if possible, but at least we logged it.
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application exiting gracefully.");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }

}
