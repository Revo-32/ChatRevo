using System.Globalization;

namespace ChatRevo.Core;

public sealed record BroadcastSummary(bool? IsLive, int? ViewerCount)
{
    public string CountText => IsLive == true && ViewerCount is >= 0
        ? ViewerCount.Value.ToString("N0", CultureInfo.InvariantCulture) : "—";
    public string DotColor => IsLive == true ? "#65D88A" : "#8C8982";
}
