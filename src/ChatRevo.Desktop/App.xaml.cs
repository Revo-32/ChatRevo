using System.Windows.Threading;
using ChatRevo.Desktop.Services;
using ChatRevo.Desktop.Views;

namespace ChatRevo.Desktop;

public partial class App : Application
{
    internal AppController Controller { get; private set; } = null!;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandled;
        var path = Environment.GetEnvironmentVariable("CHATREVO_DATA_DIR") ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatRevo", "UserData");
        Controller = new AppController(path);
        Controller.Start();
        if (e.Args.Contains("--smoke-test")) _ = SmokeTest.RunAsync(Controller);
    }
    private void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        if (Environment.GetCommandLineArgs().Contains("--smoke-test"))
        {
            var diagnostics = Environment.GetEnvironmentVariable("CHATREVO_SMOKE_DIR");
            if (diagnostics is not null) { Directory.CreateDirectory(diagnostics); File.AppendAllText(Path.Combine(diagnostics,"unhandled.txt"), e.Exception.ToString()+Environment.NewLine); }
        }
        Controller?.Report("작업을 완료하지 못했습니다. 설정 창에서 다시 시도해주세요. (" + e.Exception.GetType().Name + ")");
    }
    protected override void OnExit(ExitEventArgs e) { Controller?.Dispose(); base.OnExit(e); }
}
