using Microsoft.Web.WebView2.Core;

namespace ChatRevo.Desktop.Services;

public sealed class BrowserService(string dataDirectory)
{
    private Task<CoreWebView2Environment>? _environment;
    public Task<CoreWebView2Environment> EnvironmentAsync() => _environment ??=
        CoreWebView2Environment.CreateAsync(null, Path.Combine(dataDirectory, "WebView2"));

    public static void Configure(CoreWebView2 core, Action<string> status, bool allowPreview = false)
    {
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;
        core.Settings.AreHostObjectsAllowed = false;
        core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
        core.DownloadStarting += (_, e) => { e.Cancel = true; status("채팅 창에서 파일 다운로드는 지원하지 않습니다."); };
        core.NavigationStarting += (_, e) =>
        {
            if (NavigationPolicy.IsAllowed(e.Uri) || allowPreview && (e.Uri == "about:blank" || e.Uri.StartsWith("https://chatrevo.local/", StringComparison.Ordinal))) return;
            e.Cancel = true;
            status("이 링크는 일반 브라우저에서 열어주세요.");
        };
        core.NewWindowRequested += (_, e) => { e.Handled = true; status("로그인과 추가 인증은 설정 창의 네이버 로그인에서 진행해주세요."); };
    }
}
