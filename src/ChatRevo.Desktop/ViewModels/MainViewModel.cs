using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ChatRevo.Desktop.ViewModels;

public sealed class ActionCommand(Action action) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => action();
}

public sealed class MainViewModel : INotifyPropertyChanged
{
    private AppSettings _settings;
    private string _channelInput;
    private string _status = "연결 안 됨";
    private string _channelName = "강홍타";
    private string _liveStatus = "방송 정보 확인 전";
    private string _updateStatus = "배포 저장소 연결 전";
    private bool _locked;
    private BroadcastSummary _broadcast = new(null, null);
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? AppearanceChanged;
    public MainViewModel(AppSettings settings) { _settings = settings; _channelInput = settings.ChannelId; }
    public AppSettings Settings => _settings;
    public IReadOnlyList<FontOption> Fonts => FontCatalog.All;
    public IReadOnlyList<EffectOption> Animations => FontCatalog.Effects;
    public string FontId { get => _settings.FontId; set { Change(_settings with { FontId=value }); Changed(nameof(FontAttribution)); Changed(nameof(SampleFont)); } }
    public string? FontAttribution => FontCatalog.Find(FontId).Attribution;
    public System.Windows.Media.FontFamily SampleFont => FontFamilyFor(FontCatalog.Find(FontId), false);
    public static System.Windows.Media.FontFamily FontFamilyFor(FontOption font, bool bold)
    {
        var file = bold ? font.BoldFile ?? font.RegularFile : font.RegularFile;
        var directory = Path.GetDirectoryName(Path.Combine(AppContext.BaseDirectory,"Assets","Fonts",file))!;
        return new(new Uri(directory + Path.DirectorySeparatorChar), "./#" + (bold ? font.NativeBold : font.NativeRegular));
    }
    public string MessageAnimation { get => _settings.MessageAnimation; set { Change(_settings with {MessageAnimation=value}); Changed(nameof(AnimationEnabled)); } }
    public bool AnimationEnabled => MessageAnimation != "none";
    public double AnimationDuration { get => _settings.AnimationDuration; set => Change(_settings with {AnimationDuration=value}); }
    public double AnimationDistance { get => _settings.AnimationDistance; set => Change(_settings with {AnimationDistance=value}); }
    public string AppVersion => typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    public string VersionLabel => "v" + AppVersion;
    public string ChannelInput { get => _channelInput; set { _channelInput = value; Changed(); } }
    public string Status { get => _status; set { _status = value; Changed(); } }
    public string ChannelName { get => _channelName; set { _channelName = value; Changed(); } }
    public string LiveStatus { get => _liveStatus; set { _liveStatus = value; Changed(); SetBroadcast(null, null); } }
    public string ViewerCountText => _broadcast.CountText;
    public string LiveDotColor => _broadcast.DotColor;
    public void SetBroadcast(bool? live, int? count)
    {
        _broadcast = new(live, count);
        Changed(nameof(ViewerCountText)); Changed(nameof(LiveDotColor));
    }
    public string UpdateStatus { get => _updateStatus; set { _updateStatus = value; Changed(); } }
    public string LockLabel => _locked ? "잠금 해제" : "위치 잠금 · 클릭 통과";
    public bool Locked { get => _locked; set { _locked = value; Changed(); Changed(nameof(LockLabel)); } }
    public double FontSize { get => _settings.FontSize; set => Change(_settings with { FontSize = value }); }
    public double BackgroundOpacity { get => _settings.BackgroundOpacity; set => Change(_settings with { BackgroundOpacity = value }); }
    public bool Bold { get => _settings.Bold; set => Change(_settings with { Bold = value }); }
    public bool ShowHeader { get => _settings.ShowHeader; set => Change(_settings with { ShowHeader = value }); }
    public bool TextShadow { get => _settings.TextShadow; set => Change(_settings with { TextShadow = value }); }
    public double OutlineWidth { get => _settings.OutlineWidth; set => Change(_settings with { OutlineWidth = value }); }
    public double ShadowOpacity { get => _settings.ShadowOpacity; set => Change(_settings with { ShadowOpacity = value }); }
    public double ShadowBlur { get => _settings.ShadowBlur; set => Change(_settings with { ShadowBlur = value }); }
    public double ShadowDistance { get => _settings.ShadowDistance; set => Change(_settings with { ShadowDistance = value }); }
    public double MessageBackgroundOpacity { get => _settings.MessageBackgroundOpacity; set => Change(_settings with { MessageBackgroundOpacity = value }); }
    public double LineHeight { get => _settings.LineHeight; set => Change(_settings with { LineHeight = value }); }
    public double MessageSpacing { get => _settings.MessageSpacing; set => Change(_settings with { MessageSpacing = value }); }
    public bool UniformNicknameColor { get => _settings.UniformNicknameColor; set => Change(_settings with { UniformNicknameColor = value }); }
    public string TextColor { get => _settings.TextColor; set => Change(_settings with { TextColor = value }); }
    public string NicknameColor { get => _settings.NicknameColor; set => Change(_settings with { NicknameColor = value }); }
    public string OutlineColor { get => _settings.OutlineColor; set => Change(_settings with { OutlineColor = value }); }
    public string ShadowColor { get => _settings.ShadowColor; set => Change(_settings with { ShadowColor = value }); }
    public string MessageBackgroundColor { get => _settings.MessageBackgroundColor; set => Change(_settings with { MessageBackgroundColor = value }); }
    public bool NativeAppearance { get => _settings.NativeAppearance; set { Change(_settings with { NativeAppearance = value }); Changed(nameof(CustomStyling)); } }
    public bool CustomStyling => !_settings.NativeAppearance;
    public bool AutoUpdate { get => _settings.AutoUpdate; set => Change(_settings with { AutoUpdate = value }); }
    public string BackgroundColor => _settings.BackgroundColor;
    public ICommand ConnectCommand { get; set; } = null!;
    public ICommand PreviewCommand { get; set; } = null!;
    public ICommand LoginCommand { get; set; } = null!;
    public ICommand LogoutCommand { get; set; } = null!;
    public ICommand LockCommand { get; set; } = null!;
    public ICommand VisibilityCommand { get; set; } = null!;
    public ICommand ReloadCommand { get; set; } = null!;
    public ICommand ColorCommand { get; set; } = null!;
    public ICommand TextColorCommand { get; set; } = null!;
    public ICommand NicknameColorCommand { get; set; } = null!;
    public ICommand OutlineColorCommand { get; set; } = null!;
    public ICommand ShadowColorCommand { get; set; } = null!;
    public ICommand MessageBackgroundColorCommand { get; set; } = null!;
    public ICommand UpdateCommand { get; set; } = null!;
    public ICommand RestartCommand { get; set; } = null!;
    public ICommand ExitCommand { get; set; } = null!;
    public void SetChannel(string id) { _settings = _settings with { ChannelId = id }; }
    public void SetColor(string color) { _settings = _settings with { BackgroundColor = color }; Changed(nameof(BackgroundColor)); AppearanceChanged?.Invoke(); }
    public void SetBounds(double x, double y, double w, double h) => _settings = _settings with { OverlayLeft = x, OverlayTop = y, OverlayWidth = w, OverlayHeight = h };
    private void Change(AppSettings settings, [CallerMemberName] string? property = null) { _settings = settings.Normalize(); Changed(property); AppearanceChanged?.Invoke(); }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
