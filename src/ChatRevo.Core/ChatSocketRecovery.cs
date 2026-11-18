namespace ChatRevo.Core;

// Track transport lifecycle, including overlapping connections during a site reconnect.
// Silence on an open socket is normal, especially in a small channel.
public sealed class ChatSocketRecovery
{
    private readonly HashSet<string> _connecting = [];
    private readonly HashSet<string> _open = [];
    private TimeSpan? _missingSince;
    private TimeSpan? _healthySince;
    private int _attempts;

    public void NewDocument()
    {
        _connecting.Clear();
        _open.Clear();
        _missingSince = null;
        _healthySince = null;
    }

    public void NewChannel()
    {
        NewDocument();
        _attempts = 0;
    }

    public void Connecting(string id, string? address, TimeSpan now)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != "wss" ||
            !uri.Host.EndsWith(".chat.naver.com", StringComparison.OrdinalIgnoreCase)) return;
        _connecting.Add(id);
        if (_open.Count == 0) _missingSince ??= now;
    }

    public void Opened(string id, TimeSpan now)
    {
        if (!_connecting.Remove(id)) return;
        _open.Add(id);
        _missingSince = null;
        _healthySince ??= now;
    }

    public void Closed(string id, TimeSpan now)
    {
        if (!_connecting.Remove(id) && !_open.Remove(id)) return;
        if (_open.Count != 0) return;
        _missingSince ??= now;
        _healthySince = null;
    }

    public bool TakeRetry(TimeSpan now)
    {
        if (_healthySince is { } healthy && now - healthy >= TimeSpan.FromMinutes(2)) _attempts = 0;
        if (_missingSince is not { } since || now - since < TimeSpan.FromSeconds(30 * (1 << _attempts))) return false;
        _attempts = Math.Min(3, _attempts + 1);
        NewDocument();
        return true;
    }
}
