using Microsoft.Web.WebView2.Wpf;
using ChatRevo.Desktop.Services;

namespace ChatRevo.Desktop.Views;

public sealed class LoginWindow : Window
{
    private readonly WebView2 _web = new();
    private readonly AppController _controller;
    public LoginWindow(AppController controller)
    {
        _controller = controller; Title = "Chat Revo · 네이버 공식 로그인";
        Width = 560; Height = 760; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = _web;
        Loaded += async (_, _) =>
        {
            try
            {
                await _web.EnsureCoreWebView2Async(await controller.Browsers.EnvironmentAsync());
                BrowserService.Configure(_web.CoreWebView2, controller.Report);
                _web.CoreWebView2.NavigationCompleted += (_, _) =>
                {
                    if (_web.Source?.Host == "chzzk.naver.com") controller.Report("치지직 페이지로 돌아왔습니다. 로그인 상태를 확인한 뒤 이 창을 닫아주세요.");
                };
                _web.CoreWebView2.Navigate("https://nid.naver.com/nidlogin.login?url=" + Uri.EscapeDataString("https://chzzk.naver.com/"));
            }
            catch (Exception ex) { controller.Report("로그인 화면을 열지 못했습니다. " + ex.GetType().Name); Close(); }
        };
        Closed += (_, _) => { _web.Dispose(); _controller.Overlay.Reload(); };
    }
}
