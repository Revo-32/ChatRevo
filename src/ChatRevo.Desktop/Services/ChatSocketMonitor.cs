using System.Diagnostics;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace ChatRevo.Desktop.Services;

internal sealed class ChatSocketMonitor : IDisposable
{
    private readonly ChatSocketRecovery _recovery = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly List<(CoreWebView2DevToolsProtocolEventReceiver Receiver,
        EventHandler<CoreWebView2DevToolsProtocolEventReceivedEventArgs> Handler)> _subscriptions = [];
    private bool _enabled;

    public async Task ObserveAsync(CoreWebView2 core)
    {
        Subscribe(core, "webSocketCreated", (id, data) =>
            _recovery.Connecting(id, data.GetProperty("url").GetString(), _clock.Elapsed));
        Subscribe(core, "webSocketHandshakeResponseReceived", (id, data) =>
        {
            if (data.GetProperty("response").GetProperty("status").GetDouble() == 101)
                _recovery.Opened(id, _clock.Elapsed);
        });
        Subscribe(core, "webSocketClosed", (id, _) => _recovery.Closed(id, _clock.Elapsed));
        await core.CallDevToolsProtocolMethodAsync("Network.enable", "{}");
    }

    private void Subscribe(CoreWebView2 core, string name, Action<string, JsonElement> onEvent)
    {
        var receiver = core.GetDevToolsProtocolEventReceiver("Network." + name);
        void Handle(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs args)
        {
            if (!_enabled) return;
            using var json = JsonDocument.Parse(args.ParameterObjectAsJson);
            var data = json.RootElement;
            if (data.TryGetProperty("requestId", out var id) && id.GetString() is { } key) onEvent(key, data);
        }
        receiver.DevToolsProtocolEventReceived += Handle;
        _subscriptions.Add((receiver, Handle));
    }

    public void BeginDocument(bool isChat)
    {
        _enabled = isChat;
        _recovery.NewDocument();
    }

    public void ChangeChannel() { _enabled = false; _recovery.NewChannel(); }
    public bool TakeRetry() => _enabled && _recovery.TakeRetry(_clock.Elapsed);

    public void Dispose()
    {
        foreach (var (receiver, handler) in _subscriptions) receiver.DevToolsProtocolEventReceived -= handler;
        _subscriptions.Clear();
        _enabled = false;
    }
}
