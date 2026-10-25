namespace ChatRevo.Core;

public static class NavigationPolicy
{
    public static bool IsAllowed(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo)) return false;
        return uri.Host is "chzzk.naver.com" or "nid.naver.com" or "www.naver.com";
    }

    public static bool IsChat(string? url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        uri.Scheme == "https" && uri.Host == "chzzk.naver.com" && uri.AbsolutePath.StartsWith("/live/", StringComparison.Ordinal) &&
        uri.AbsolutePath.EndsWith("/chat", StringComparison.Ordinal) && ChannelAddress.TryParse(url,out _);
}
