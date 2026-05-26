$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$assetsDir = Join-Path $repoRoot 'DaylogDockExtension\Assets'
New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null

Add-Type -AssemblyName System.Drawing

$ColorPage = [System.Drawing.Color]::FromArgb(255, 255, 246, 232)
$ColorPageEdge = [System.Drawing.Color]::FromArgb(255, 235, 224, 205)
$ColorPageBorder = [System.Drawing.Color]::FromArgb(255, 72, 66, 58)
$ColorLine = [System.Drawing.Color]::FromArgb(255, 58, 52, 46)
$ColorToday = [System.Drawing.Color]::FromArgb(255, 210, 118, 0)

function New-RoundedRectPath {
    param(
        [System.Drawing.RectangleF]$Rect,
        [float]$Radius
    )

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = [Math]::Min($Radius * 2, [Math]::Min($Rect.Width, $Rect.Height))
    if ($diameter -le 0) {
        $path.AddRectangle($Rect)
        $path.CloseFigure()
        return $path
    }

    $arc = [System.Drawing.RectangleF]::new($Rect.X, $Rect.Y, $diameter, $diameter)
    $path.AddArc($arc, 180, 90)
    $arc.X = $Rect.Right - $diameter
    $path.AddArc($arc, 270, 90)
    $arc.Y = $Rect.Bottom - $diameter
    $path.AddArc($arc, 0, 90)
    $arc.X = $Rect.X
    $path.AddArc($arc, 90, 90)
    $path.CloseFigure()
    return $path
}

function Get-CanvasRect {
    param([int]$Width, [int]$Height)

    $size = [Math]::Min($Width, $Height)
    $left = ($Width - $size) / 2.0
    $top = ($Height - $size) / 2.0
    return [pscustomobject]@{
        Size = $size
        Left = $left
        Top = $top
        Square = [System.Drawing.RectangleF]::new([float]$left, [float]$top, [float]$size, [float]$size)
    }
}

function Get-MaximizedPageRect {
    param(
        $Canvas,
        [float]$InsetRatio
    )

    $inset = [Math]::Max(1, [int][Math]::Round($Canvas.Size * $InsetRatio))
    return [System.Drawing.RectangleF]::new(
        $Canvas.Square.X + $inset,
        $Canvas.Square.Y + $inset,
        $Canvas.Square.Width - (2 * $inset),
        $Canvas.Square.Height - (2 * $inset)
    )
}

function Draw-DaylogMark {
    param(
        [System.Drawing.Graphics]$Graphics,
        [int]$Width,
        [int]$Height
    )

    $canvas = Get-CanvasRect $Width $Height
    $size = $canvas.Size

    if ($size -le 48) {
        Draw-DaylogMarkSmall $Graphics $canvas
    }
    elseif ($size -le 128) {
        Draw-DaylogMarkMedium $Graphics $canvas
    }
    else {
        Draw-DaylogMarkLarge $Graphics $canvas
    }
}

function Draw-DaylogPageContent {
    param(
        [System.Drawing.Graphics]$Graphics,
        [System.Drawing.RectangleF]$Page,
        [float]$Scale,
        [bool]$UseGradients,
        [bool]$PixelCrisp
    )

    $pageRadius = if ($PixelCrisp) {
        [Math]::Max(1, [int](6 * $Scale))
    } else {
        [float]([Math]::Max(4, 14 * $Scale))
    }

    $pagePath = New-RoundedRectPath $Page $pageRadius
    if ($UseGradients) {
        $pageBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            $Page,
            $ColorPage,
            $ColorPageEdge,
            [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
        )
    } else {
        $pageBrush = New-Object System.Drawing.SolidBrush($ColorPage)
    }

    $Graphics.FillPath($pageBrush, $pagePath)
    $pageBrush.Dispose()

    $borderWidth = if ($PixelCrisp) { 1 } else { [Math]::Max(1, 2 * $Scale) }
    $borderPen = New-Object System.Drawing.Pen($ColorPageBorder, [float]$borderWidth)
    $Graphics.DrawPath($borderPen, $pagePath)
    $borderPen.Dispose()

    $lineBrush = New-Object System.Drawing.SolidBrush($ColorLine)
    $lineH = if ($PixelCrisp) { [Math]::Max(1, [int](3 * $Scale)) } else { [Math]::Max(2, [int](6 * $Scale)) }
    $lineW = $Page.Width * 0.64
    $lineX = $Page.X + (($Page.Width - $lineW) / 2)
    foreach ($yFactor in @(0.26, 0.40, 0.54)) {
        $line = [System.Drawing.RectangleF]::new(
            [float]$lineX,
            [float]($Page.Y + ($Page.Height * $yFactor)),
            [float]$lineW,
            [float]$lineH
        )
        if ($PixelCrisp) {
            $Graphics.FillRectangle($lineBrush, [int]$line.X, [int]$line.Y, [int]$line.Width, [int]$line.Height)
        } else {
            $linePath = New-RoundedRectPath $line ([float]($lineH / 2))
            $Graphics.FillPath($lineBrush, $linePath)
            $linePath.Dispose()
        }
    }
    $lineBrush.Dispose()

    $dot = if ($PixelCrisp) {
        [Math]::Max(2, [int]($Page.Width * 0.11))
    } else {
        [Math]::Max(3, [int]($Page.Width * 0.1))
    }
    $dotBrush = New-Object System.Drawing.SolidBrush($ColorToday)
    if ($PixelCrisp) {
        $Graphics.FillRectangle(
            $dotBrush,
            [int]($Page.X + ($Page.Width * 0.1)),
            [int]($Page.Bottom - $dot - ($Page.Height * 0.1)),
            $dot,
            $dot
        )
    } else {
        $Graphics.FillEllipse(
            $dotBrush,
            [float]($Page.X + ($Page.Width * 0.1)),
            [float]($Page.Bottom - $dot - ($Page.Height * 0.1)),
            [float]$dot,
            [float]$dot
        )
    }
    $dotBrush.Dispose()
    $pagePath.Dispose()
}

function Draw-DaylogMarkSmall {
    param(
        [System.Drawing.Graphics]$Graphics,
        $Canvas
    )

    $Graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    $Graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::None
    $Graphics.Clear([System.Drawing.Color]::Transparent)

    $page = Get-MaximizedPageRect $Canvas 0.04
    Draw-DaylogPageContent $Graphics $page ($Canvas.Size / 48.0) $false $true
}

function Draw-DaylogMarkMedium {
    param(
        [System.Drawing.Graphics]$Graphics,
        $Canvas
    )

    $Graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $Graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $Graphics.Clear([System.Drawing.Color]::Transparent)

    $page = Get-MaximizedPageRect $Canvas 0.03
    Draw-DaylogPageContent $Graphics $page ($Canvas.Size / 128.0) $false $false
}

function Draw-DaylogMarkLarge {
    param(
        [System.Drawing.Graphics]$Graphics,
        $Canvas
    )

    $Graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $Graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $Graphics.Clear([System.Drawing.Color]::Transparent)

    $page = Get-MaximizedPageRect $Canvas 0.02
    Draw-DaylogPageContent $Graphics $page ($Canvas.Size / 512.0) $true $false
}

function Save-DaylogPng {
    param(
        [string]$Name,
        [int]$Width,
        [int]$Height
    )

    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    Draw-DaylogMark $graphics $Width $Height
    $path = Join-Path $assetsDir $Name
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
}

function New-DaylogPngBytes {
    param([int]$Size)

    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    Draw-DaylogMark $graphics $Size $Size
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $stream.ToArray()
    $stream.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
    return $bytes
}

function Save-DaylogIco {
    $sizes = @(16, 24, 32, 48, 64, 128, 256)
    $entries = @()
    $offset = 6 + ($sizes.Count * 16)

    foreach ($size in $sizes) {
        [byte[]]$bytes = @(New-DaylogPngBytes $size)
        $entries += [pscustomobject]@{
            Size = $size
            Bytes = $bytes
            Offset = $offset
        }
        $offset += $bytes.Length
    }

    $stream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($stream)
    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]$entries.Count)

    foreach ($entry in $entries) {
        $directorySize = if ($entry.Size -eq 256) { 0 } else { $entry.Size }
        $writer.Write([byte]$directorySize)
        $writer.Write([byte]$directorySize)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$entry.Bytes.Length)
        $writer.Write([UInt32]$entry.Offset)
    }

    foreach ($entry in $entries) {
        $writer.Write([byte[]]$entry.Bytes)
    }

    $writer.Flush()
    [System.IO.File]::WriteAllBytes((Join-Path $assetsDir 'Daylog.ico'), $stream.ToArray())
    $writer.Dispose()
    $stream.Dispose()
}

Save-DaylogPng 'Square44x44Logo.targetsize-24_altform-unplated.png' 24 24
Save-DaylogPng 'LockScreenLogo.scale-200.png' 48 48
Save-DaylogPng 'StoreLogo.png' 50 50
Save-DaylogPng 'Square44x44Logo.png' 44 44
Save-DaylogPng 'Square44x44Logo.scale-200.png' 88 88
Save-DaylogPng 'Square150x150Logo.png' 150 150
Save-DaylogPng 'Square150x150Logo.scale-200.png' 300 300
Save-DaylogPng 'SplashScreen.png' 400 400
Save-DaylogPng 'SplashScreen.scale-200.png' 400 400
Save-DaylogPng 'Wide310x150Logo.png' 620 150
Save-DaylogPng 'Wide310x150Logo.scale-200.png' 620 300
Save-DaylogIco

$publicDir = Join-Path $repoRoot 'DaylogDockExtension\Public'
New-Item -ItemType Directory -Force -Path $publicDir | Out-Null
$publicIconPath = Join-Path $publicDir 'icon.png'
$publicBitmap = New-Object System.Drawing.Bitmap(128, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$publicGraphics = [System.Drawing.Graphics]::FromImage($publicBitmap)
Draw-DaylogMark $publicGraphics 128 128
$publicBitmap.Save($publicIconPath, [System.Drawing.Imaging.ImageFormat]::Png)
$publicGraphics.Dispose()
$publicBitmap.Dispose()

Write-Host "Generated Daylog icon assets in $assetsDir"
Write-Host "Generated CmdPal extension icon at $publicIconPath"
