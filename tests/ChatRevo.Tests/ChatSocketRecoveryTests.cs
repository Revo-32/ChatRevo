using ChatRevo.Core;
using Xunit;

namespace ChatRevo.Tests;

public class ChatSocketRecoveryTests
{
    private static readonly string Chat = "wss://kr-ss1.chat.naver.com/chat";
    private static TimeSpan At(int seconds) => TimeSpan.FromSeconds(seconds);

    [Fact]
    public void AnOpenQuietSocketIsHealthyAndSiteReconnectCancelsRecovery()
    {
        var state = new ChatSocketRecovery();
        state.Connecting("a", Chat, At(0));
        state.Opened("a", At(1));
        Assert.False(state.TakeRetry(At(3600)));
        state.Closed("a", At(3601));
        state.Connecting("b", Chat, At(3610));
        state.Opened("b", At(3612));
        Assert.False(state.TakeRetry(At(3650)));
        state.Connecting("c", Chat, At(3660));
        state.Opened("c", At(3661));
        state.Closed("b", At(3662));
        Assert.False(state.TakeRetry(At(3800)));
    }

    [Fact]
    public void RepeatedFailedHandshakesDoNotPostponeRetryAndReloadBacksOff()
    {
        var state = new ChatSocketRecovery();
        state.Connecting("a", Chat, At(0));
        state.Opened("a", At(1));
        state.Closed("a", At(10));
        state.Connecting("b", Chat, At(25));
        state.Closed("b", At(26));
        Assert.False(state.TakeRetry(At(39)));
        Assert.True(state.TakeRetry(At(40)));
        Assert.False(state.TakeRetry(At(41)));
        state.NewDocument();
        state.Connecting("c", Chat, At(45));
        Assert.False(state.TakeRetry(At(75)));
        Assert.True(state.TakeRetry(At(105)));
    }

    [Fact]
    public void OtherSocketsAndOldChannelEventsCannotTriggerReload()
    {
        var state = new ChatSocketRecovery();
        state.Connecting("unrelated","wss://chat.naver.com.example.org/", At(0));
        Assert.False(state.TakeRetry(At(60)));
        state.Connecting("a", Chat, At(61));
        state.NewChannel();
        state.Opened("a", At(62));
        state.Closed("a", At(63));
        Assert.False(state.TakeRetry(At(120)));
    }
}
