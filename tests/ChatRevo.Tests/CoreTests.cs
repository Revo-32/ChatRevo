using ChatRevo.Core;
using Xunit;

namespace ChatRevo.Tests;

public class CoreTests
{
    private const string Id="daa441054054971cbd2488618efc9105";
    [Theory]
    [InlineData(Id)]
    [InlineData("https://chzzk.naver.com/"+Id)]
    [InlineData("https://chzzk.naver.com/live/"+Id+"?from=share")]
    [InlineData("https://chzzk.naver.com/chat/"+Id)]
    [InlineData("https://chzzk.naver.com/live/"+Id+"/chat")]
    public void AcceptsChannelForms(string input) { Assert.True(ChannelAddress.TryParse(input,out var id)); Assert.Equal(Id,id); }
    [Theory]
    [InlineData("https://chzzk.naver.com.evil.test/"+Id)]
    [InlineData("https://evil@chzzk.naver.com/"+Id)]
    [InlineData("http://chzzk.naver.com/"+Id)]
    [InlineData("https://chzzk.naver.com:8443/"+Id)]
    [InlineData("https://chzzk.naver.com/video/123")]
    [InlineData("javascript:alert(1)")]
    public void RejectsUnexpectedAddresses(string input) => Assert.False(ChannelAddress.TryParse(input,out _));
    [Theory]
    [InlineData("https://nid.naver.com/nidlogin.login",true)]
    [InlineData("https://nid.naver.com.evil.test/",false)]
    [InlineData("file:///C:/Windows/win.ini",false)]
    [InlineData("https://evil.test/",false)]
    public void RestrictsBrowserNavigation(string url,bool allowed) => Assert.Equal(allowed,NavigationPolicy.IsAllowed(url));
    [Fact]
    public void RestoresSettingsAndPreservesCorruptFile()
    {
        var dir=Path.Combine(Path.GetTempPath(),"ChatRevo-tests-"+Guid.NewGuid());
        try
        {
            var store=new SettingsStore(dir);
            store.Save(new AppSettings {FontSize=34,BackgroundOpacity=0,OverlayLeft=-1400});
            var reloaded=new SettingsStore(dir).Load();
            Assert.Equal(34,reloaded.FontSize); Assert.Equal(0,reloaded.BackgroundOpacity); Assert.Equal(-1400,reloaded.OverlayLeft);
            File.WriteAllText(store.FilePath,"invalid json");
            Assert.Equal(20,store.Load().FontSize);
            Assert.NotNull(store.LoadWarning);
            Assert.Single(Directory.GetFiles(dir,"*.recovery-*"));
            Assert.Equal("invalid json",File.ReadAllText(store.FilePath));
        }
        finally { if(Directory.Exists(dir)) Directory.Delete(dir,true); }
    }
    [Fact]
    public void NormalizesInvalidStoredValues()
    {
        var settings=new AppSettings {FontSize=double.NaN,BackgroundOpacity=-50,BackgroundColor="bad",OverlayWidth=0,ChannelId="file:///secret"}.Normalize();
        Assert.Equal(20,settings.FontSize); Assert.Equal(0,settings.BackgroundOpacity); Assert.Equal(280,settings.OverlayWidth);
        Assert.Equal("#10151C",settings.BackgroundColor); Assert.Empty(settings.ChannelId);
    }
    [Theory]
    [InlineData(true,4,"4","#65D88A")]
    [InlineData(true,0,"0","#65D88A")]
    [InlineData(true,1234,"1,234","#65D88A")]
    [InlineData(true,null,"—","#65D88A")]
    [InlineData(null,4,"—","#8C8982")]
    [InlineData(false,4,"—","#8C8982")]
    public void CompactHeaderDistinguishesZeroAndUnavailable(bool? live,int? count,string text,string color)
    {
        var summary=new BroadcastSummary(live,count);
        Assert.Equal(text,summary.CountText); Assert.Equal(color,summary.DotColor);
    }
    [Fact]
    public void AddsReadabilityDefaultsWithoutResettingPreviousSettings()
    {
        var old=System.Text.Json.JsonSerializer.Deserialize<AppSettings>("""
            {"SchemaVersion":1,"FontSize":26,"TextShadow":false,"BackgroundOpacity":18,"OverlayLeft":200,"OverlayTop":300}
            """)!.Normalize();
        Assert.Equal(26,old.FontSize); Assert.False(old.TextShadow); Assert.Equal(18,old.BackgroundOpacity);
        Assert.Equal(200,old.OverlayLeft); Assert.Equal(300,old.OverlayTop);
        Assert.Equal(1,old.OutlineWidth); Assert.Equal("#FFFFFF",old.TextColor);
        Assert.Equal(0,old.MessageBackgroundOpacity); Assert.Equal(1.45,old.LineHeight);
    }
    [Fact]
    public void ReadabilitySettingsRoundTripAndRejectInvalidCssValues()
    {
        var original=new AppSettings {TextColor="#ffeedd",OutlineWidth=1.75,ShadowOpacity=72,ShadowBlur=4.5,
            ShadowDistance=2,MessageBackgroundOpacity=40,MessageBackgroundColor="#102030",UniformNicknameColor=true,
            NicknameColor="#FFF090",LineHeight=1.7,MessageSpacing=9};
        var normalized=original.Normalize();
        var loaded=System.Text.Json.JsonSerializer.Deserialize<AppSettings>(System.Text.Json.JsonSerializer.Serialize(normalized));
        Assert.Equal(normalized,loaded); Assert.Equal("#FFEEDD",normalized.TextColor);
        var invalid=new AppSettings {OutlineWidth=double.PositiveInfinity,ShadowBlur=500,LineHeight=0,
            TextColor="red;display:none",ShadowColor=null!,MessageBackgroundOpacity=-5}.Normalize();
        Assert.Equal(1,invalid.OutlineWidth); Assert.Equal(12,invalid.ShadowBlur); Assert.Equal(1.1,invalid.LineHeight);
        Assert.Equal("#FFFFFF",invalid.TextColor); Assert.Equal("#000000",invalid.ShadowColor); Assert.Equal(0,invalid.MessageBackgroundOpacity);
    }
}
