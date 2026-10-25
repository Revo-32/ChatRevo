$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore,WindowsBase
$brandDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'src/ChatRevo.Desktop/Assets/Brand'
[xml]$svg = Get-Content -LiteralPath (Join-Path $brandDir 'mark.svg') -Raw
$frames = @()
foreach ($size in @(16,20,24,32,40,48,64,128,256,512)) {
    $visual = [System.Windows.Media.DrawingVisual]::new()
    $context = $visual.RenderOpen()
    $context.PushTransform([System.Windows.Media.ScaleTransform]::new($size / 128.0,$size / 128.0))
    foreach ($path in $svg.svg.path) {
        $geometry = [System.Windows.Media.Geometry]::Parse($path.d)
        $brush = [System.Windows.Media.BrushConverter]::new().ConvertFromString($path.fill)
        $context.DrawGeometry($brush,$null,$geometry)
    }
    $context.Pop()
    $context.Close()
    $bitmap = [System.Windows.Media.Imaging.RenderTargetBitmap]::new($size,$size,96,96,[System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [System.IO.MemoryStream]::new()
    $encoder.Save($stream)
    $bytes = $stream.ToArray()
    $stream.Dispose()
    if ($size -eq 512) { [System.IO.File]::WriteAllBytes((Join-Path $brandDir 'mark.png'),$bytes) }
    if ($size -le 256) { $frames += [pscustomobject]@{Size=$size;Bytes=$bytes} }
}
$icon = [System.IO.File]::Create((Join-Path $brandDir 'ChatRevo.ico'))
$writer = [System.IO.BinaryWriter]::new($icon)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
    $offset = 6 + 16 * $frames.Count
    foreach ($frame in $frames) {
        $dimension = if ($frame.Size -eq 256) { 0 } else { $frame.Size }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$frame.Bytes.Length); $writer.Write([uint32]$offset)
        $offset += $frame.Bytes.Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame.Bytes) }
} finally { $writer.Dispose(); $icon.Dispose() }
