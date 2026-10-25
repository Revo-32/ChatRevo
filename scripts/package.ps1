param(
    [string]$Version = '',
    [string]$RepositoryUrl = ''
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not $Version) {
    [xml]$buildProperties = Get-Content -LiteralPath (Join-Path $projectRoot 'Directory.Build.props') -Raw
    $Version = [string]$buildProperties.Project.PropertyGroup.Version
}
$publishDir = Join-Path $projectRoot 'artifacts/publish'
$releaseDir = Join-Path $projectRoot 'artifacts/releases'
if ($Version -notmatch '^\d+\.\d+\.\d+(-[A-Za-z0-9.-]+)?$') { throw '잘못된 버전 형식입니다.' }
dotnet publish (Join-Path $projectRoot 'src/ChatRevo.Desktop') -c Release -r win-x64 --self-contained true -p:Version=$Version -o $publishDir
if ($LASTEXITCODE -ne 0) { throw '앱 게시 실패' }
if ($RepositoryUrl) {
    $repoUri = [Uri]$RepositoryUrl
    if ($repoUri.Scheme -ne 'https' -or $repoUri.Host -ne 'github.com' -or $repoUri.AbsolutePath -notmatch '^/[^/]+/[^/]+/?$' -or $repoUri.Query -or $repoUri.Fragment -or $repoUri.UserInfo) { throw '올바른 GitHub 저장소 URL이 필요합니다.' }
    $distributionPath = Join-Path $publishDir 'distribution.json'
    $config = Get-Content -LiteralPath $distributionPath -Raw | ConvertFrom-Json
    $config.githubRepository = $RepositoryUrl
    $config | ConvertTo-Json | Set-Content -LiteralPath $distributionPath -Encoding utf8
}
Push-Location $projectRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw '패키징 도구 복원 실패' }
    dotnet vpk pack --packId ChatRevo --packVersion $Version --packDir $publishDir --mainExe ChatRevo.exe --packTitle 'Chat Revo' --runtime win-x64 --icon (Join-Path $projectRoot 'src/ChatRevo.Desktop/Assets/Brand/ChatRevo.ico') --outputDir $releaseDir
    if ($LASTEXITCODE -ne 0) { throw '패키징 실패' }
} finally { Pop-Location }
