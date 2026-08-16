using SuperLighter.App.Services;
using SuperLighter.App.UI;

namespace SuperLighter.App;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Local\SuperLighter.SingleInstance";
    private const string OpenSettingsEventName = @"Local\SuperLighter.OpenSettings";

    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
        {
            return SelfTests.Run() ? 0 : 1;
        }

        if (args.Contains("--ui-preview", StringComparer.OrdinalIgnoreCase))
        {
            using var previewForm = new SettingsForm(new AppSettings(), _ => { });
            Application.Run(previewForm);
            return 0;
        }

        if (args.Contains("--exit-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunExitTest();
        }

        using var singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            SignalOpenSettings();
            return 0;
        }

        using var openSettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, OpenSettingsEventName);
        using var context = new SuperLighterApplicationContext(openSettingsEvent);

        EventHandler processExitHandler = (_, _) => context.RestoreDisplayEffects();
        AppDomain.CurrentDomain.ProcessExit += processExitHandler;
        Application.ThreadException += (_, exceptionArgs) =>
        {
            context.RestoreDisplayEffects();
            MessageBox.Show(
                $"The app encountered an error and restored the display settings.\n\n{exceptionArgs.Exception.Message}",
                "SuperLighter",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Application.Exit();
        };

        try
        {
            Application.Run(context);
            return 0;
        }
        finally
        {
            context.RestoreDisplayEffects();
            AppDomain.CurrentDomain.ProcessExit -= processExitHandler;
        }
    }

    private static void SignalOpenSettings()
    {
        try
        {
            using var openSettingsEvent = EventWaitHandle.OpenExisting(OpenSettingsEventName);
            openSettingsEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
        }
    }

    private static int RunExitTest()
    {
        using var openSettingsEvent = new EventWaitHandle(
            false,
            EventResetMode.AutoReset);
        using var context = new SuperLighterApplicationContext(openSettingsEvent);
        using var exitTimer = new System.Windows.Forms.Timer { Interval = 250 };
        exitTimer.Tick += (_, _) =>
        {
            exitTimer.Stop();
            context.ExitApplication();
        };
        exitTimer.Start();
        Application.Run(context);
        return 0;
    }
}
