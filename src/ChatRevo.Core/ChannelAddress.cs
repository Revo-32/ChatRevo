using System.Text.RegularExpressions;

namespace ChatRevo.Core;

public static partial class ChannelAddress
{
    [GeneratedRegex("^[a-fA-F0-9]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex ChannelIdPattern();

    public static bool TryParse(string? input, out string channelId)
    {
        channelId = "";
        var value = input?.Trim() ?? "";
        if (ChannelIdPattern().IsMatch(value)) { channelId = value.ToLowerInvariant(); return true; }
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            uri.Host != "chzzk.naver.com" || !uri.IsDefaultPort || uri.UserInfo.Length != 0) return false;
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var candidate = segments.Length == 1 ? segments[0] :
            segments.Length == 2 && segments[0] is "live" or "chat" ? segments[1] :
            segments.Length == 3 && segments[0] == "live" && segments[2] == "chat" ? segments[1] : "";
        if (!ChannelIdPattern().IsMatch(candidate)) return false;
        channelId = candidate.ToLowerInvariant();
        return true;
    }

    public static string ChatUrl(string id) => TryParse(id, out var clean)
        ? $"https://chzzk.naver.com/live/{clean}/chat" : throw new ArgumentException("올바른 채널 ID가 아닙니다.");
}
