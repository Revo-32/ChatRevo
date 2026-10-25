using System.Text.Json;
using Velopack;
using Velopack.Sources;

namespace ChatRevo.Desktop.Services;

public sealed class UpdateService
{
    private UpdateManager? _manager;
    private bool _busy;
    public async Task CheckAsync(Action<string> report)
    {
        if (_busy) return;
        _busy = true;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"distribution.json")));
            var repo = doc.RootElement.GetProperty("githubRepository").GetString();
            if (string.IsNullOrWhiteSpace(repo)) { report("배포 저장소 연결 전 · 첫 릴리스에서 활성화됩니다."); return; }
            if (!Uri.TryCreate(repo, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "github.com" || uri.AbsolutePath.Split('/',StringSplitOptions.RemoveEmptyEntries).Length != 2)
                throw new InvalidOperationException("Invalid update source");
            _manager ??= new UpdateManager(new GithubSource(repo, null, false));
            if (!_manager.IsInstalled) { report("포터블 실행 중 · 자동 업데이트는 설치형에서 사용할 수 있습니다."); return; }
            if (_manager.UpdatePendingRestart is not null) { report("다운로드 완료 · 다음 시작 또는 재시작 시 적용됩니다."); return; }
            report("새 버전을 확인하고 있습니다…");
            var update = await _manager.CheckForUpdatesAsync();
            if (update is null) { report("최신 버전입니다."); return; }
            report("업데이트 다운로드 중… 게임은 계속 이용할 수 있습니다.");
            await _manager.DownloadUpdatesAsync(update);
            report("다운로드 완료 · 다음 시작 또는 재시작 시 적용됩니다.");
        }
        catch (Exception) { report("업데이트를 확인하지 못했습니다. 현재 버전은 계속 사용할 수 있습니다."); }
        finally { _busy = false; }
    }
    public void Restart(Action beforeExit, Action<string> report)
    {
        if (_manager is null || _manager.UpdatePendingRestart is null) { report("적용할 업데이트가 없습니다."); return; }
        try { beforeExit(); _manager.ApplyUpdatesAndRestart(_manager.UpdatePendingRestart); }
        catch (Exception) { report("업데이트 적용을 시작하지 못했습니다. 앱을 다시 실행해주세요."); }
    }
}
