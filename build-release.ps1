$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SrcDir = Join-Path $RootDir "src"
$ReleaseDir = Join-Path $RootDir "Release"
$PortableDir = Join-Path $ReleaseDir "DesktopFences-Portable"

New-Item -ItemType Directory -Force -Path $ReleaseDir | Out-Null
New-Item -ItemType Directory -Force -Path $PortableDir | Out-Null

# Generate High-Definition 256x256 + Multi-Size 32-bit ARGB App Icon (.ico)
$IconPath = Join-Path $SrcDir "DesktopFences.ico"
Add-Type -AssemblyName System.Drawing
function New-FenceIconFrame([int]$sz) {
    $bmp = New-Object System.Drawing.Bitmap($sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $pad = [Math]::Max(1, [int]($sz * 0.06))
    $rect = New-Object System.Drawing.Rectangle($pad, $pad, ($sz - 2 * $pad), ($sz - 2 * $pad))
    $radius = [Math]::Max(2, [int]($sz * 0.22))

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(255, 30, 58, 138),
        [System.Drawing.Color]::FromArgb(255, 14, 165, 233),
        45.0)
    $g.FillPath($grad, $path)

    # 4 crisp white/cyan fence grid tiles inside
    $inner = [int]($sz * 0.24)
    $gap = [Math]::Max(1, [int]($sz * 0.08))
    $tileSz = [Math]::Max(2, [int](($sz - 2 * $inner - $gap) / 2))
    $tileBrush1 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(245, 255, 255, 255))
    $tileBrush2 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(220, 186, 230, 253))

    $g.FillRectangle($tileBrush1, $inner, $inner, $tileSz, $tileSz)
    $g.FillRectangle($tileBrush2, ($inner + $tileSz + $gap), $inner, $tileSz, $tileSz)
    $g.FillRectangle($tileBrush2, $inner, ($inner + $tileSz + $gap), $tileSz, $tileSz)
    $g.FillRectangle($tileBrush1, ($inner + $tileSz + $gap), ($inner + $tileSz + $gap), $tileSz, $tileSz)

    $grad.Dispose(); $tileBrush1.Dispose(); $tileBrush2.Dispose(); $path.Dispose(); $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return ,$ms.ToArray()
}

$sizes = @(256, 64, 48, 32, 16)
$pngFrames = @()
foreach ($s in $sizes) { $pngFrames += ,(New-FenceIconFrame $s) }

$icoStream = [System.IO.File]::Create($IconPath)
$bw = New-Object System.IO.BinaryWriter($icoStream)
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$sizes.Count)

$offset = 6 + (16 * $sizes.Count)
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $bytes = $pngFrames[$i]
    $wByte = if ($s -eq 256) { [byte]0 } else { [byte]$s }
    $bw.Write($wByte)
    $bw.Write($wByte)
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]32)
    $bw.Write([uint32]$bytes.Length)
    $bw.Write([uint32]$offset)
    $offset += $bytes.Length
}
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $bw.Write($pngFrames[$i])
}
$bw.Flush()
$icoStream.Close()

$CscPath = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $CscPath)) {
    throw "Built-in Windows C# compiler not found at $CscPath"
}

$WpfDir = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\WPF"
$Refs = @(
    "/r:System.dll",
    "/r:System.Core.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll",
    "/r:System.Web.Extensions.dll",
    "/r:System.Xaml.dll",
    "/r:Microsoft.CSharp.dll",
    "/r:$WpfDir\PresentationCore.dll",
    "/r:$WpfDir\PresentationFramework.dll",
    "/r:$WpfDir\WindowsBase.dll"
)

$MainExe = Join-Path $ReleaseDir "DesktopFences.exe"
$ManifestPath = Join-Path $SrcDir "app.manifest"
$MainSrc = Join-Path $SrcDir "DesktopFences.cs"

Write-Host "[1/4] Compiling DesktopFences.exe (x64 + 256px HD Icons)..." -ForegroundColor Cyan
& $CscPath /nologo /target:winexe /optimize+ /platform:x64 "/win32icon:$IconPath" "/win32manifest:$ManifestPath" "/out:$MainExe" @Refs "$MainSrc"
if ($LASTEXITCODE -ne 0) { throw "Failed to compile DesktopFences.exe" }

Write-Host "[2/4] Building One-Click Installer (DesktopFences-Setup-v1.0.0-win-x64.exe)..." -ForegroundColor Cyan
$SetupExe = Join-Path $ReleaseDir "DesktopFences-Setup-v1.0.0-win-x64.exe"
$SetupSrc = Join-Path $SrcDir "SetupInstaller.cs"
& $CscPath /nologo /target:winexe /optimize+ /platform:x64 "/win32icon:$IconPath" "/win32manifest:$ManifestPath" "/resource:$MainExe,DesktopFences.exe" "/out:$SetupExe" @Refs "$SetupSrc"
if ($LASTEXITCODE -ne 0) { throw "Failed to compile Setup Installer" }

Write-Host "[3/4] Packaging Portable Edition (DesktopFences-Portable-v1.0.0-win-x64.zip)..." -ForegroundColor Cyan
Copy-Item $MainExe -Destination (Join-Path $PortableDir "DesktopFences.exe") -Force
Set-Content -Path (Join-Path $PortableDir "portable.mode") -Value "Portable Mode Enabled - JSON layouts stored locally in ./Data"
$PortableZip = Join-Path $ReleaseDir "DesktopFences-Portable-v1.0.0-win-x64.zip"
if (Test-Path $PortableZip) { Remove-Item $PortableZip -Force }
Compress-Archive -Path "$PortableDir\*" -DestinationPath $PortableZip -Force

Write-Host "[4/4] Generating SHA256SUMS.txt..." -ForegroundColor Cyan
$Hashes = @()
foreach ($file in @($SetupExe, $PortableZip, $MainExe)) {
    $h = Get-FileHash -Path $file -Algorithm SHA256
    $Hashes += "$($h.Hash.ToLower())  $(Split-Path $file -Leaf)"
}
$Hashes | Set-Content -Path (Join-Path $ReleaseDir "SHA256SUMS.txt") -Encoding UTF8

Write-Host "`nBUILD SUCCESSFUL! Ready-to-install files are in: $ReleaseDir" -ForegroundColor Green
Get-ChildItem $ReleaseDir | Select-Object Name, Length
