using System.Text.Json;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace ChatRevo.Desktop.Services;

internal static class SmokeTest
{
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left,Top,Right,Bottom; }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint hwnd,out Rect rect);
    public static async Task RunAsync(AppController app)
    {
        var dir = Environment.GetEnvironmentVariable("CHATREVO_SMOKE_DIR") ?? Path.Combine(app.Store.DirectoryPath,"smoke");
        Directory.CreateDirectory(dir);
        var result = new Dictionary<string,object?>();
        try
        {
            await app.PreviewAsync(); await Task.Delay(2500);
            result["startupStatus"] = app.ViewModel.Status;
            result["initializationError"] = app.Overlay.InitializationError;
            result["preview"] = await app.Overlay.Core!.ExecuteScriptAsync("document.body.innerText.includes('미리보기')");
            var regressionPath = Environment.GetEnvironmentVariable("CHATREVO_BROWSER_TEST_SCRIPT");
            if (regressionPath is not null)
            {
                await app.Overlay.Core.ExecuteScriptAsync(File.ReadAllText(regressionPath));
                string regression = "null";
                for (var attempt=0; attempt<120 && regression is "null"; attempt++)
                {
                    await Task.Delay(250);
                    regression=await app.Overlay.Core.ExecuteScriptAsync("window.__chatRevoRegression ?? null");
                }
                result["browserRegression"]=JsonSerializer.Deserialize<JsonElement>(regression);
                using var report=JsonDocument.Parse(regression);
                if (report.RootElement.ValueKind!=JsonValueKind.Object || !report.RootElement.GetProperty("ok").GetBoolean())
                    throw new InvalidOperationException("브라우저 회귀 검사 실패: "+regression);
                await app.Overlay.Core.ExecuteScriptAsync("window.createInputProbe();");
                await Task.Delay(200);
                await app.Overlay.Core.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent", "{\"type\":\"mouseWheel\",\"x\":100,\"y\":100,\"deltaY\":-250,\"deltaX\":0}");
                await Task.Delay(400);
                result["wheelPausesHistory"] = await CheckScript(app,"!inputProbe.follow.following && inputProbe.box.scrollTop < -50");
                await app.Overlay.Core.ExecuteScriptAsync("inputProbe.saved=inputProbe.box.scrollTop;inputProbe.add();");
                await Task.Delay(200);
                result["wheelHistoryStable"] = await CheckScript(app,"Math.abs(inputProbe.saved-inputProbe.box.scrollTop)<2");
                await app.Overlay.Core.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent", "{\"type\":\"mouseWheel\",\"x\":100,\"y\":100,\"deltaY\":800,\"deltaX\":0}");
                await Task.Delay(400);
                result["wheelResumesBottom"] = await CheckScript(app,"inputProbe.follow.following && inputProbe.follow.distance<2");
                await app.Overlay.Core.ExecuteScriptAsync("inputProbe.dispose();");
            }
            app.ViewModel.FontSize=28; app.ViewModel.BackgroundOpacity=25;
            await Task.Delay(700);
            result["fontSize"] = await app.Overlay.Core.ExecuteScriptAsync("getComputedStyle(document.body).fontSize");
            result["headerScales"] = app.Overlay.HeaderChannel.FontSize == 30 && app.Overlay.HeaderCount.FontSize == 22;
            var defaults = new AppSettings();
            app.ViewModel.OutlineWidth=1.5; app.ViewModel.OutlineColor="#001122";
            app.ViewModel.ShadowOpacity=80; app.ViewModel.ShadowBlur=4; app.ViewModel.ShadowDistance=2;
            app.ViewModel.ShadowColor="#101010"; app.ViewModel.TextColor="#FFEEDD";
            app.ViewModel.UniformNicknameColor=true; app.ViewModel.NicknameColor="#FFF090";
            app.ViewModel.LineHeight=1.7; app.ViewModel.MessageSpacing=9;
            app.ViewModel.MessageBackgroundOpacity=45; app.ViewModel.MessageBackgroundColor="#102030";
            await Task.Delay(700);
            result["previewReadability"] = await CheckScript(app, "(()=>{const s=getComputedStyle(document.querySelector('.row')), n=getComputedStyle(document.querySelector('.name'));return s.webkitTextStrokeWidth==='3px' && s.webkitTextStrokeColor==='rgb(0, 17, 34)' && s.color==='rgb(255, 238, 221)' && n.color==='rgb(255, 240, 144)' && s.backgroundColor==='rgba(16, 32, 48, 0.45)' && s.textShadow.includes('4px') && s.lineHeight==='47.6px' && s.marginBottom==='9px';})()");
            app.ViewModel.TextShadow=false; app.ViewModel.OutlineWidth=0;
            app.ViewModel.MessageBackgroundOpacity=0; app.ViewModel.UniformNicknameColor=false;
            await Task.Delay(700);
            result["effectsDisableCleanly"] = await CheckScript(app, "(()=>{const s=getComputedStyle(document.querySelector('.row')), n=getComputedStyle(document.querySelector('.name'));return s.webkitTextStrokeWidth==='0px' && s.textShadow==='none' && s.backgroundColor==='rgba(0, 0, 0, 0)' && n.color==='rgb(245, 147, 77)';})()");
            RestoreAppearance(app, defaults);
            await Task.Delay(700);
            result["readabilityPersists"] = app.Store.Load().OutlineWidth == defaults.OutlineWidth;
            app.Main.Activate(); await Task.Delay(200);
            var foreground=NativeWindow.GetForegroundWindow();
            app.ToggleLock();
            result["clickThrough"] = NativeWindow.IsClickThrough(app.Overlay);
            result["hitTestPassesThrough"] = NativeWindow.HitTestPassesThrough(app.Overlay);
            result["noFocusSteal"] = foreground == NativeWindow.GetForegroundWindow();
            app.ToggleLock();
            result["unlocked"] = !NativeWindow.IsClickThrough(app.Overlay);
            CaptureWpf(app.Main,Path.Combine(dir,"settings.png"));
            app.Main.Navigation.SelectedIndex=1;
            await Task.Delay(300);
            CaptureWpf(app.Main,Path.Combine(dir,"appearance.png"));
            app.Main.Appearance.FontPicker.SelectedValue="wanted";
            app.Main.Appearance.AnimationPicker.SelectedValue="rise";
            await Task.Delay(800);
            result["fontPickerBinds"] = app.ViewModel.FontId == "wanted" && app.ViewModel.MessageAnimation == "rise";
            result["previewFontSelection"] = await CheckScript(app,"getComputedStyle(document.querySelector('.row')).fontFamily.includes('ChatRevo_wanted')");
            CaptureWpf(app.Main,Path.Combine(dir,"appearance-selected.png"));
            app.Main.Appearance.FontPicker.IsDropDownOpen=true;
            await Task.Delay(200);
            Capture(app.Main,Path.Combine(dir,"font-menu.png"));
            app.Main.Appearance.FontPicker.IsDropDownOpen=false;
            RestoreAppearance(app,defaults);
            await Task.Delay(500);
            app.Main.Appearance.Scroll.ScrollToEnd();
            await Task.Delay(200);
            CaptureWpf(app.Main,Path.Combine(dir,"appearance-bottom.png"));
            app.Main.Appearance.Scroll.ScrollToTop();
            app.Main.Navigation.SelectedIndex=0;
            Capture(app.Overlay,Path.Combine(dir,"preview.png"));
            app.ViewModel.FontSize=20; app.ViewModel.BackgroundOpacity=35;
            await app.ConnectAsync(); await Task.Delay(12000);
            var core=app.Overlay.Core!;
            result["scrollLayout"]=await core.ExecuteScriptAsync("JSON.stringify((()=>{let n=document.querySelector('[class*=\"_list_bottom_\"]');const a=[];while(n){const s=getComputedStyle(n);a.push({cls:n.className,overflow:s.overflowY,direction:s.flexDirection,display:s.display,h:n.clientHeight,sh:n.scrollHeight,top:n.scrollTop,children:[...n.children].slice(0,5).map(e=>({cls:e.className,y:e.getBoundingClientRect().y,h:e.getBoundingClientRect().height}))});n=n.parentElement;}return a;})())");
            result["source"]=core.Source;
            result["liveStatus"]=app.ViewModel.LiveStatus;
            result["channelName"]=app.ViewModel.ChannelName;
            result["compactHeader"]=app.Overlay.HeaderCount.Text==app.ViewModel.ViewerCountText &&
                Math.Abs(app.Overlay.HeaderChannel.TranslatePoint(new System.Windows.Point(0, app.Overlay.HeaderChannel.ActualHeight / 2),app.Overlay).Y-
                         app.Overlay.HeaderCount.TranslatePoint(new System.Windows.Point(0, app.Overlay.HeaderCount.ActualHeight / 2),app.Overlay).Y)<2;
            result["headerHierarchy"] = app.Overlay.HeaderChannel.FontSize == 22 && app.Overlay.HeaderCount.FontSize == 16;
            var channelName = app.ViewModel.ChannelName;
            var overlayWidth = app.Overlay.Width;
            app.ViewModel.ChannelName = "아주 긴 채널 이름으로 좁은 오버레이의 헤더 표시 확인";
            app.Overlay.Width = 280;
            await Task.Delay(200);
            result["narrowHeaderFits"] = app.Overlay.HeaderCount.TranslatePoint(new System.Windows.Point(app.Overlay.HeaderCount.ActualWidth, 0), app.Overlay).X <= app.Overlay.ActualWidth - 19;
            app.ViewModel.ChannelName = channelName;
            app.Overlay.Width = overlayWidth;
            await Task.Delay(200);
            result["page"]=await core.ExecuteScriptAsync("JSON.stringify({title:document.title,text:document.body.innerText.slice(0,3500),classes:[...new Set([...document.querySelectorAll('[class]')].map(e=>e.className).filter(c=>typeof c==='string'))].slice(0,130),font:document.fonts.check('20px ChatRevoJamsil')})");
            result["styleAttached"]=await core.ExecuteScriptAsync("!!document.getElementById('chat-revo-style')");
            result["messageStyle"]=await core.ExecuteScriptAsync("JSON.stringify([...document.querySelectorAll('[data-chat-revo-message]')].slice(-3).map(e=>({size:getComputedStyle(e).fontSize,font:getComputedStyle(e).fontFamily})))");
            result["liveReadability"]=await CheckScript(app,"(()=>{const e=document.querySelector('[data-chat-revo-message]');if(!e)return false;const s=getComputedStyle(e);return s.webkitTextStrokeWidth==='2px'&&s.paintOrder==='stroke'&&s.textShadow!=='none';})()");
            result["surfaces"]=await core.ExecuteScriptAsync("document.querySelectorAll('[data-chat-revo-surface]').length");
            result["composerHidden"]=await core.ExecuteScriptAsync("(()=>{const e=document.querySelector('[data-chat-revo-composer]');return !!e&&getComputedStyle(e).display==='none'})()");
            result["rankingHidden"]=await core.ExecuteScriptAsync("(()=>{const e=document.querySelector('[data-chat-revo-ranking]');return !!e&&getComputedStyle(e).display==='none'})()");
            result["fontFaces"]=await core.ExecuteScriptAsync("JSON.stringify([...document.fonts].filter(f=>f.family.includes('Jamsil')).map(f=>({family:f.family,status:f.status})))");
            result["fontLoad"]=await core.ExecuteScriptAsync("document.fonts.load('20px ChatRevoJamsil').then(()=>window.chrome.webview.postMessage({kind:'fontDiag',ok:true})).catch(e=>window.chrome.webview.postMessage({kind:'fontDiag',error:e.message}))");
            result["chatStructure"]=await core.ExecuteScriptAsync("JSON.stringify([...document.querySelectorAll('[class*=\"_chatting_message_\"]')].slice(0,1).map(e=>({html:e.outerHTML,parents:[e.parentElement?.className,e.parentElement?.parentElement?.className]})))");
            app.ViewModel.NativeAppearance=true;
            await Task.Delay(500);
            result["nativeStyleRestored"]=await core.ExecuteScriptAsync("document.getElementById('chat-revo-style').textContent.includes('font-face')===false");
            result["rankingHiddenInNative"]=await core.ExecuteScriptAsync("(()=>{const e=document.querySelector('[data-chat-revo-ranking]');return !!e&&getComputedStyle(e).display==='none'})()");
            app.ViewModel.NativeAppearance=false;
            await Task.Delay(500);
            await using (var stream=File.Create(Path.Combine(dir,"chat-web.png"))) await core.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,stream);
            Capture(app.Overlay,Path.Combine(dir,"chat-overlay.png"));
            app.ToggleLock();
            await Task.Delay(200);
            Capture(app.Overlay,Path.Combine(dir,"chat-locked.png"));
            app.ToggleLock();
            app.Save(); result["settingsSaved"]=File.Exists(app.Store.FilePath);
        }
        catch(Exception ex) { result["error"]=ex.ToString(); }
        finally { File.WriteAllText(Path.Combine(dir,"results.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions {WriteIndented=true})); app.Exit(); }
    }
    private static void Capture(Window window,string path)
    {
        GetWindowRect(new WindowInteropHelper(window).Handle,out var rect);
        using var bitmap=new Bitmap(Math.Max(1,rect.Right-rect.Left),Math.Max(1,rect.Bottom-rect.Top));
        using var g=Graphics.FromImage(bitmap);
        g.CopyFromScreen(rect.Left,rect.Top,0,0,bitmap.Size);
        bitmap.Save(path,ImageFormat.Png);
    }
    private static async Task<bool> CheckScript(AppController app,string script)
    {
        var answer=await app.Overlay.Core!.ExecuteScriptAsync(script);
        if(answer!="true") throw new InvalidOperationException("화면 검증 실패: "+script+" 결과: "+answer);
        return true;
    }
    private static void RestoreAppearance(AppController app,AppSettings settings)
    {
        foreach(var property in app.ViewModel.GetType().GetProperties().Where(p=>p.CanWrite))
        {
            var setting=typeof(AppSettings).GetProperty(property.Name);
            if(setting is not null) property.SetValue(app.ViewModel,setting.GetValue(settings));
        }
    }
    private static void CaptureWpf(Window window,string path)
    {
        var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var stream=File.Create(path); encoder.Save(stream);
    }
}
