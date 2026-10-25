using System.Windows.Threading;
using ChatRevo.Desktop.ViewModels;
using ChatRevo.Desktop.Views;
using Forms = System.Windows.Forms;

namespace ChatRevo.Desktop.Services;

public sealed class AppController : IDisposable
{
    public SettingsStore Store { get; }
    public BrowserService Browsers { get; }
    public MainViewModel ViewModel { get; }
    public OverlayWindow Overlay { get; private set; } = null!;
    public MainWindow Main { get; private set; } = null!;
    public bool Exiting { get; private set; }
    private LoginWindow? _login;
    private Forms.NotifyIcon? _tray;
    private readonly UpdateService _updates = new();
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private bool _connecting;
    public AppController(string dataPath)
    {
        Store = new SettingsStore(dataPath); Browsers = new BrowserService(dataPath);
        ViewModel = new MainViewModel(Store.Load());
        ViewModel.ConnectCommand = new ActionCommand(() => _ = ConnectAsync());
        ViewModel.PreviewCommand = new ActionCommand(() => _ = PreviewAsync());
        ViewModel.LoginCommand = new ActionCommand(Login);
        ViewModel.LogoutCommand = new ActionCommand(() => _ = LogoutAsync());
        ViewModel.LockCommand = new ActionCommand(ToggleLock);
        ViewModel.VisibilityCommand = new ActionCommand(ToggleVisibility);
        ViewModel.ReloadCommand = new ActionCommand(() => Overlay.Reload());
        ViewModel.ColorCommand = new ActionCommand(PickColor);
        ViewModel.TextColorCommand = new ActionCommand(() => PickColor("본문 색상", ViewModel.TextColor, value => ViewModel.TextColor=value));
        ViewModel.NicknameColorCommand = new ActionCommand(() => PickColor("닉네임 색상", ViewModel.NicknameColor, value => ViewModel.NicknameColor=value));
        ViewModel.OutlineColorCommand = new ActionCommand(() => PickColor("외곽선 색상", ViewModel.OutlineColor, value => ViewModel.OutlineColor=value));
        ViewModel.ShadowColorCommand = new ActionCommand(() => PickColor("그림자 색상", ViewModel.ShadowColor, value => ViewModel.ShadowColor=value));
        ViewModel.MessageBackgroundColorCommand = new ActionCommand(() => PickColor("메시지 배경 색상", ViewModel.MessageBackgroundColor, value => ViewModel.MessageBackgroundColor=value));
        ViewModel.UpdateCommand = new ActionCommand(() => _ = _updates.CheckAsync(s => ViewModel.UpdateStatus=s));
        ViewModel.RestartCommand = new ActionCommand(() => _updates.Restart(Save, s => ViewModel.UpdateStatus=s));
        ViewModel.ExitCommand = new ActionCommand(Exit);
        _saveTimer.Tick += async (_, _) =>
        {
            _saveTimer.Stop(); Save();
            try { await Overlay.ApplyStyleAsync(); } catch (Exception) { Report("스타일을 적용하지 못했습니다. 새로고침해주세요."); }
        };
        ViewModel.AppearanceChanged += QueueSave;
    }
    public void Start()
    {
        Main = new MainWindow(this); Application.Current.MainWindow=Main;
        Overlay = new OverlayWindow(this);
        Overlay.BoundsChanged += QueueSave;
        Main.Show();
        var menu = new Forms.ContextMenuStrip { Renderer=new Forms.ToolStripProfessionalRenderer(new TrayColors()), ForeColor=System.Drawing.Color.FromArgb(236,234,230) };
        menu.Items.Add("설정 열기", null, (_, _) => Dispatch(ShowSettings));
        menu.Items.Add("오버레이 표시 / 숨기기", null, (_, _) => Dispatch(ToggleVisibility));
        menu.Items.Add("잠금 / 잠금 해제", null, (_, _) => Dispatch(ToggleLock));
        menu.Items.Add("종료", null, (_, _) => Dispatch(Exit));
        _tray = new Forms.NotifyIcon { Text="Chat Revo", Icon=new System.Drawing.Icon(Path.Combine(AppContext.BaseDirectory,"Assets","Brand","ChatRevo.ico")), Visible=true, ContextMenuStrip=menu };
        _tray.DoubleClick += (_, _) => Dispatch(ShowSettings);
        if (Store.LoadWarning is not null) Report(Store.LoadWarning);
        if (ViewModel.AutoUpdate) _ = _updates.CheckAsync(s => ViewModel.UpdateStatus=s);
    }
    private static void Dispatch(Action action) => Application.Current.Dispatcher.Invoke(action);
    public void Report(string message) => ViewModel.Status = message;
    public void ShowSettings() { Main.Show(); Main.WindowState=WindowState.Normal; Main.Activate(); }
    private void ShowOverlay() { if (!Overlay.IsVisible) Overlay.Show(); Overlay.SetLocked(ViewModel.Locked); }
    public async Task ConnectAsync()
    {
        if (_connecting) return;
        if (!ChannelAddress.TryParse(ViewModel.ChannelInput,out var id)) { Report("올바른 치지직 채널 URL 또는 32자리 채널 ID를 입력해주세요."); return; }
        _connecting=true;
        try
        {
            ViewModel.SetChannel(id); ViewModel.ChannelName = id == "daa441054054971cbd2488618efc9105" ? "강홍타" : "채널 정보 확인 중";
            ViewModel.LiveStatus="방송 정보 연결 중…"; Report("공식 채팅 페이지를 연결하고 있습니다…");
            ShowOverlay(); await Overlay.NavigateAsync(id); Save();
        }
        catch (Exception ex) { Report("채팅 연결을 시작하지 못했습니다. " + ex.GetType().Name); }
        finally { _connecting=false; }
    }
    public async Task PreviewAsync()
    {
        if (_connecting) return;
        _connecting=true;
        try { ViewModel.ChannelName="Chat Revo"; ViewModel.LiveStatus="미리보기 · 실제 방송 아님"; ShowOverlay(); await Overlay.NavigateAsync(null); }
        catch (Exception ex) { Report("미리보기를 열지 못했습니다. " + ex.GetType().Name); }
        finally { _connecting=false; }
    }
    public void ToggleLock()
    {
        ViewModel.Locked = !ViewModel.Locked; ShowOverlay(); Overlay.SetLocked(ViewModel.Locked);
        if (Overlay.Core is null) _ = PreviewAsync();
        Report(ViewModel.Locked ? "잠금 완료 · 마우스가 게임으로 통과합니다. Ctrl + Alt + L로 해제하세요." : "편집 모드 · 상단을 드래그하거나 모서리에서 크기를 조절하세요.");
    }
    public void ToggleVisibility()
    {
        if (Overlay.IsVisible) Overlay.Hide(); else { ShowOverlay(); if (Overlay.Core is null) _ = PreviewAsync(); }
    }
    private void Login()
    {
        if (_login is { IsLoaded:true }) { _login.Activate(); return; }
        _login = new LoginWindow(this) { Owner=Main }; _login.Show();
    }
    private async Task LogoutAsync()
    {
        try
        {
            _login?.Close();
            ShowOverlay();
            await Overlay.InitializeAsync();
            await Overlay.Core!.Profile.ClearBrowsingDataAsync(Microsoft.Web.WebView2.Core.CoreWebView2BrowsingDataKinds.AllProfile);
            Overlay.Reload(); Report("Chat Revo의 로그인 세션을 지웠습니다. 다른 브라우저의 로그인은 유지됩니다.");
        }
        catch (Exception) { Report("로그아웃을 완료하지 못했습니다. 다시 시도해주세요."); }
    }
    private void PickColor()
    {
        PickColor("전체 배경 색상", ViewModel.BackgroundColor, ViewModel.SetColor);
    }
    private void PickColor(string title, string color, Action<string> apply)
    {
        var picker=new ColorWindow(color) {Owner=Main, Title=title};
        if (picker.ShowDialog()==true) apply(picker.SelectedColor);
    }
    private void QueueSave() { if (Exiting) return; _saveTimer.Stop(); _saveTimer.Start(); }
    public void Save()
    {
        if (Overlay is null) return;
        ViewModel.SetBounds(Overlay.Left,Overlay.Top,Overlay.Width,Overlay.Height);
        try { Store.Save(ViewModel.Settings); } catch (Exception ex) when(ex is IOException or UnauthorizedAccessException) { Report("설정을 저장하지 못했습니다. 저장 폴더 권한을 확인해주세요."); }
    }
    public void Exit() { if (Exiting) return; Save(); Exiting=true; _login?.Close(); Overlay.Close(); Main.Close(); Application.Current.Shutdown(); }
    public void Dispose() { _saveTimer.Stop(); _tray?.Dispose(); }
}
