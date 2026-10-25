using System.Threading;
using Velopack;

namespace ChatRevo.Desktop;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        var smoke = args.Contains("--smoke-test");
        using var mutex = new Mutex(true, smoke ? "Local\\ChatRevo.Smoke" : "Local\\ChatRevo.Desktop", out var first);
        if (!first) { MessageBox.Show("Chat Revo가 이미 실행 중입니다. 트레이 아이콘에서 설정을 열어주세요.", "Chat Revo"); return; }
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
