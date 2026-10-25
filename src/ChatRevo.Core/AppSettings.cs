namespace ChatRevo.Core;

public sealed record AppSettings
{
    public int SchemaVersion { get; init; } = 1;
    public string ChannelId { get; init; } = "daa441054054971cbd2488618efc9105";
    public double FontSize { get; init; } = 20;
    public bool Bold { get; init; }
    public string BackgroundColor { get; init; } = "#10151C";
    public double BackgroundOpacity { get; init; } = 35;
    public bool ShowHeader { get; init; } = true;
    public bool TextShadow { get; init; } = true;
    public string TextColor { get; init; } = "#FFFFFF";
    public bool UniformNicknameColor { get; init; }
    public string NicknameColor { get; init; } = "#FFD09A";
    public double OutlineWidth { get; init; } = 1;
    public string OutlineColor { get; init; } = "#000000";
    public double ShadowOpacity { get; init; } = 90;
    public double ShadowBlur { get; init; } = 3;
    public double ShadowDistance { get; init; } = 1;
    public string ShadowColor { get; init; } = "#000000";
    public double MessageBackgroundOpacity { get; init; } = 0;
    public string MessageBackgroundColor { get; init; } = "#000000";
    public double LineHeight { get; init; } = 1.45;
    public double MessageSpacing { get; init; } = 4;
    public bool NativeAppearance { get; init; }
    public bool AutoUpdate { get; init; } = true;
    public double OverlayLeft { get; init; } = 1100;
    public double OverlayTop { get; init; } = 110;
    public double OverlayWidth { get; init; } = 400;
    public double OverlayHeight { get; init; } = 620;

    public AppSettings Normalize() => this with
    {
        ChannelId = ChannelAddress.TryParse(ChannelId, out var id) ? id : "",
        FontSize = Clamp(FontSize, 12, 48, 20),
        BackgroundOpacity = Clamp(BackgroundOpacity, 0, 100, 35),
        BackgroundColor = System.Text.RegularExpressions.Regex.IsMatch(BackgroundColor ?? "", "^#[0-9a-fA-F]{6}$") ? BackgroundColor! : "#10151C",
        TextColor = Color(TextColor, "#FFFFFF"),
        NicknameColor = Color(NicknameColor, "#FFD09A"),
        OutlineColor = Color(OutlineColor, "#000000"),
        ShadowColor = Color(ShadowColor, "#000000"),
        MessageBackgroundColor = Color(MessageBackgroundColor, "#000000"),
        OutlineWidth = Clamp(OutlineWidth, 0, 3, 1),
        ShadowOpacity = Clamp(ShadowOpacity, 0, 100, 90),
        ShadowBlur = Clamp(ShadowBlur, 0, 12, 3),
        ShadowDistance = Clamp(ShadowDistance, 0, 6, 1),
        MessageBackgroundOpacity = Clamp(MessageBackgroundOpacity, 0, 100, 0),
        LineHeight = Clamp(LineHeight, 1.1, 2, 1.45),
        MessageSpacing = Clamp(MessageSpacing, 0, 16, 4),
        OverlayWidth = Clamp(OverlayWidth, 280, 2000, 400),
        OverlayHeight = Clamp(OverlayHeight, 200, 2000, 620),
        OverlayLeft = Clamp(OverlayLeft, -32000, 32000, 1100),
        OverlayTop = Clamp(OverlayTop, -32000, 32000, 110)
    };

    private static double Clamp(double n, double min, double max, double fallback) =>
        double.IsFinite(n) ? Math.Clamp(n, min, max) : fallback;
    private static string Color(string? value, string fallback) =>
        System.Text.RegularExpressions.Regex.IsMatch(value ?? "", "^#[0-9a-fA-F]{6}$") ? value!.ToUpperInvariant() : fallback;
}
