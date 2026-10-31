namespace ChatRevo.Core;

public sealed record FontOption(string Id, string Name, string RegularFile, string? BoldFile,
    string NativeRegular, string NativeBold, string? Attribution = null)
{
    public override string ToString() => Name;
}
public sealed record EffectOption(string Id, string Name)
{
    public override string ToString() => Name;
}

public static class FontCatalog
{
    public static IReadOnlyList<FontOption> All { get; } = [
        new("jamsil", "더잠실체", "TheJamsil-Regular.ttf", "TheJamsil-Bold.ttf", "The Jamsil 3", "The Jamsil 5"),
        new("wanted", "Wanted Sans", "wanted/WantedSans-Regular.ttf", "wanted/WantedSans-Bold.ttf", "Wanted Sans", "Wanted Sans"),
        new("pretendard", "Pretendard", "pretendard/Pretendard-Regular.ttf", "pretendard/Pretendard-Bold.ttf", "Pretendard", "Pretendard"),
        new("suit", "SUIT", "suit/SUIT-Regular.ttf", "suit/SUIT-Bold.ttf", "SUIT", "SUIT"),
        new("ibmplexsanskr", "IBM Plex Sans KR", "ibmplexsanskr/IBMPlexSansKR-Regular.ttf", "ibmplexsanskr/IBMPlexSansKR-Bold.ttf", "IBM Plex Sans KR", "IBM Plex Sans KR"),
        new("nanumgothic", "나눔고딕", "nanumgothic/NanumGothic-Regular.ttf", "nanumgothic/NanumGothic-Bold.ttf", "NanumGothic", "NanumGothic"),
        new("gowundodum", "고운돋움", "gowundodum/GowunDodum-Regular.ttf", null, "Gowun Dodum", "Gowun Dodum"),
        new("gowunbatang", "고운바탕", "gowunbatang/GowunBatang-Regular.ttf", "gowunbatang/GowunBatang-Bold.ttf", "Gowun Batang", "Gowun Batang"),
        new("jua", "배민 주아체", "jua/Jua-Regular.ttf", null, "Jua", "Jua"),
        new("dohyeon", "배민 도현체", "dohyeon/DoHyeon-Regular.ttf", null, "Do Hyeon", "Do Hyeon"),
        new("kimm", "한국기계연구원체", "kimm/KIMM_Light.ttf", "kimm/KIMM_bold.ttf", "KIMM L", "KIMM", "출처 – 한국기계연구원, kimm.re.kr")
    ];
    public static FontOption Find(string? id) => All.FirstOrDefault(f => f.Id == id) ?? All[0];
    public static IReadOnlyList<EffectOption> Effects { get; } = [
        new("none", "없음"), new("fade", "페이드"), new("rise", "떠오르기"),
        new("slide", "옆에서 들어오기"), new("pop", "팝")
    ];
}
