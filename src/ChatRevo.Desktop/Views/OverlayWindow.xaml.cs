using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using ChatRevo.Desktop.Services;

namespace ChatRevo.Desktop.Views;

public partial class OverlayWindow : Window
{
    private readonly AppController _controller;
    private Task? _initializing;
    private bool _disposed;
    private string _target = "https://chatrevo.local/preview.html";
    private readonly DispatcherTimer _retryTimer = new() { Interval = TimeSpan.FromSeconds(10) };
    private int _retryCount;
    private readonly DispatcherTimer _staleTimer = new() { Interval = TimeSpan.FromSeconds(15) };
    private DateTime _lastLive = DateTime.MinValue;
    public bool IsPreview { get; private set; } = true;
    public CoreWebView2? Core => Browser.CoreWebView2;
    public string? InitializationError { get; private set; }
    public event Action? BoundsChanged;
    public OverlayWindow(AppController controller)
    {
        _controller = controller;
        InitializeComponent();
        DataContext = controller.ViewModel;
        var fontDirectory = new Uri(Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts") + Path.DirectorySeparatorChar);
        HeaderChannel.FontFamily = new FontFamily(fontDirectory, "./#The Jamsil 5");
        HeaderCount.FontFamily = new FontFamily(fontDirectory, "./#The Jamsil 3");
        InfoBar.SizeChanged += (_, _) => FitHeader();
        HeaderCount.SizeChanged += (_, _) => FitHeader();
        var s = controller.ViewModel.Settings;
        Width = s.OverlayWidth; Height = s.OverlayHeight; Left = s.OverlayLeft; Top = s.OverlayTop;
        if (Left + Width < SystemParameters.VirtualScreenLeft + 40 || Left > SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 40 ||
            Top + Height < SystemParameters.VirtualScreenTop + 40 || Top > SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 40)
        { Left = Math.Max(0, SystemParameters.WorkArea.Right - Width - 24); Top = 80; }
        LocationChanged += (_, _) => BoundsChanged?.Invoke();
        SizeChanged += (_, _) => BoundsChanged?.Invoke();
        SourceInitialized += (_, _) => SetLocked(false);
        _retryTimer.Tick += (_, _) => { _retryTimer.Stop(); Reload(); };
        _staleTimer.Tick += (_, _) => { if (!IsPreview && DateTime.UtcNow - _lastLive > TimeSpan.FromSeconds(90)) _controller.ViewModel.LiveStatus = "시청자 수 갱신 지연"; };
        _staleTimer.Start();
        ApplyNativeStyle();
    }
    public Task InitializeAsync() => _initializing ??= InitializeCoreAsync();
    private async Task InitializeCoreAsync()
    {
        try
        {
            await Browser.EnsureCoreWebView2Async(await _controller.Browsers.EnvironmentAsync());
            if (_disposed) return;
            var core = Browser.CoreWebView2;
            BrowserService.Configure(core, _controller.Report, true);
            core.IsMuted = true;
            core.SetVirtualHostNameToFolderMapping("chatrevo.local", Path.Combine(AppContext.BaseDirectory, "Assets"), CoreWebView2HostResourceAccessKind.Allow);
            var assets = Path.Combine(AppContext.BaseDirectory, "Assets");
            await core.AddScriptToExecuteOnDocumentCreatedAsync(File.ReadAllText(Path.Combine(assets, "readability.js")) + "\n" + File.ReadAllText(Path.Combine(assets, "overlay.js")));
            core.NavigationCompleted += OnNavigationCompleted;
            core.WebMessageReceived += OnWebMessage;
            core.ProcessFailed += (_, _) => ShowError("브라우저 연결이 중단되었습니다. 다시 연결을 눌러주세요.");
        }
        catch (Exception ex)
        {
            InitializationError = ex.ToString();
            ShowError("WebView2 초기화 실패: " + ex.GetType().Name + ". Microsoft Edge WebView2 런타임을 확인해주세요.");
            throw;
        }
    }
    public async Task NavigateAsync(string? channelId)
    {
        IsPreview = channelId is null;
        _target = IsPreview ? "https://chatrevo.local/preview.html" : ChannelAddress.ChatUrl(channelId!);
        _retryTimer.Stop(); _retryCount = 0; _lastLive = DateTime.UtcNow;
        await InitializeAsync();
        if (_disposed) return;
        ErrorPanel.Visibility = Visibility.Collapsed;
        Browser.CoreWebView2.Navigate(_target);
    }
    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_disposed) return;
        if (!e.IsSuccess)
        {
            ShowError("네트워크 연결을 확인해주세요. 자동 재연결 중입니다.");
            _retryTimer.Interval = TimeSpan.FromSeconds(Math.Min(60, 5 * Math.Pow(2, Math.Min(_retryCount++, 4))));
            _retryTimer.Start();
            return;
        }
        _retryCount = 0;
        ErrorPanel.Visibility = Visibility.Collapsed;
        try { await ApplyStyleAsync(); _controller.Report(IsPreview ? "미리보기입니다. 실제 채팅은 채팅 연결을 눌러주세요." : "채팅 페이지를 열었습니다. 로그인은 설정 창에서 진행할 수 있습니다."); }
        catch (Exception) { _controller.Report("스타일을 적용하지 못했습니다. 새로고침을 시도해주세요."); }
    }
    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (IsPreview || !NavigationPolicy.IsChat(e.Source)) return;
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var root = doc.RootElement;
            if (root.GetProperty("channelId").GetString() != _controller.ViewModel.Settings.ChannelId) return;
            var kind = root.GetProperty("kind").GetString();
            if (kind == "liveUnavailable") { _controller.ViewModel.LiveStatus = "시청자 수 확인 불가 · 자동 재시도"; return; }
            if (kind != "live") return;
            _lastLive = DateTime.UtcNow;
            var name = root.GetProperty("channelName").GetString();
            if (!string.IsNullOrWhiteSpace(name)) _controller.ViewModel.ChannelName = name.Length > 80 ? name[..80] : name;
            var open = root.GetProperty("status").GetString() == "OPEN";
            var count = root.GetProperty("count");
            _controller.ViewModel.LiveStatus = !open ? "방송 종료" : count.TryGetInt32Safe(out var n) && n >= 0 ? $"LIVE · {n:N0}명 시청 중" : "LIVE · 시청자 수 비공개 / 확인 불가";
            _controller.ViewModel.SetBroadcast(open, count.TryGetInt32Safe(out var viewers) && viewers >= 0 ? viewers : null);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException) { }
    }
    public async Task ApplyStyleAsync()
    {
        ApplyNativeStyle();
        if (Core is null || _disposed) return;
        var s = _controller.ViewModel.Settings;
        var json = JsonSerializer.Serialize(s, new JsonSerializerOptions { PropertyNamingPolicy=JsonNamingPolicy.CamelCase });
        await Core.ExecuteScriptAsync($"window.__chatRevo?.apply({json});");
    }
    private void ApplyNativeStyle()
    {
        var s = _controller.ViewModel.Settings;
        var color = (Color)ColorConverter.ConvertFromString(s.BackgroundColor);
        color.A = (byte)Math.Round(s.BackgroundOpacity * 2.55);
        Frame.Background = new SolidColorBrush(color);
        InfoBar.Visibility = s.ShowHeader ? Visibility.Visible : Visibility.Collapsed;
        HeaderChannel.FontSize = Math.Max(18, s.FontSize + 2);
        HeaderCount.FontSize = Math.Max(14, Math.Round(s.FontSize * 0.8));
        LiveDot.Width = LiveDot.Height = Math.Max(6, Math.Round(s.FontSize * 0.35));
        FitHeader();
    }
    private void FitHeader()
    {
        // Keep the count beside the name, reserving room when the window narrows.
        HeaderChannel.MaxWidth = Math.Max(24, InfoBar.ActualWidth - InfoBar.Padding.Left -
            InfoBar.Padding.Right - HeaderCount.ActualWidth - LiveDot.Width -
            LiveDot.Margin.Left - LiveDot.Margin.Right);
    }
    public void SetLocked(bool locked)
    {
        EditBar.Visibility = locked ? Visibility.Collapsed : Visibility.Visible;
        Frame.BorderBrush = locked ? Brushes.Transparent : new SolidColorBrush(Color.FromRgb(177,106,56));
        ResizeMode = locked ? ResizeMode.NoResize : ResizeMode.CanResizeWithGrip;
        Browser.IsHitTestVisible = !locked;
        NativeWindow.SetLocked(this, locked);
    }
    public void Reload() { if (Core is not null && !_disposed) { _retryTimer.Stop(); ErrorPanel.Visibility = Visibility.Collapsed; Core.Navigate(_target); } }
    private void ShowError(string message) { ErrorText.Text = message; ErrorPanel.Visibility = Visibility.Visible; _controller.Report(message); }
    private void DragHeader(object sender, MouseButtonEventArgs e) { if (!_controller.ViewModel.Locked && e.LeftButton == MouseButtonState.Pressed) DragMove(); }
    private void LockClick(object sender, RoutedEventArgs e) => _controller.ToggleLock();
    private void RetryClick(object sender, RoutedEventArgs e) => Reload();
    protected override void OnClosed(EventArgs e) { _disposed = true; _retryTimer.Stop(); _staleTimer.Stop(); Browser.Dispose(); base.OnClosed(e); }
}

internal static class JsonExtensions
{
    public static bool TryGetInt32Safe(this JsonElement element, out int n) { n=0; return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out n); }
}
