using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;

namespace CommLib.Examples.WinUI;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            WriteStartupError("AppDomain.CurrentDomain.UnhandledException", eventArgs.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
        {
            WriteStartupError("TaskScheduler.UnobservedTaskException", eventArgs.Exception);
            eventArgs.SetObserved();
        };

        ComWrappersSupport.InitializeComWrappers();

        try
        {
            Microsoft.UI.Xaml.Application.Start(_ =>
            {
                try
                {
                    SynchronizationContext.SetSynchronizationContext(
                        new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));

                    var app = new App();
                }
                catch (Exception exception)
                {
                    WriteStartupError("Application.Start callback", exception);
                    throw;
                }
            });
        }
        catch (Exception exception)
        {
            WriteStartupError("Program.Main", exception);
            throw;
        }
    }

    private static void WriteStartupError(string origin, object? payload)
    {
        try
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "startup-error.log");
            File.AppendAllText(
                logPath,
                $"[{DateTimeOffset.Now:O}] {origin}{Environment.NewLine}{payload}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
        }
    }
}
