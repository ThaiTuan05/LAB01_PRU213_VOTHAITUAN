Add-Type -AssemblyName System.Drawing
$root = Join-Path $PSScriptRoot '../Assets/SpaceExplorer'
New-Item -ItemType Directory -Force "$root/Sprites", "$root/UI", "$root/Backgrounds" | Out-Null
function Draw-Asset($name, $width, $height, $draw) {
    $bitmap = [Drawing.Bitmap]::new($width, $height)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([Drawing.Color]::Transparent)
    & $draw $graphics
    $bitmap.Save((Join-Path $root $name), [Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
}
Draw-Asset 'Sprites/Star.png' 128 128 {
    param($g)
    $points = [Drawing.PointF[]](0..9 | ForEach-Object {
        $angle = (-90 + $_ * 36) * [Math]::PI / 180
        $radius = if ($_ % 2 -eq 0) { 51 } else { 24 }
        [Drawing.PointF]::new((64 + [Math]::Cos($angle)*$radius), (64 + [Math]::Sin($angle)*$radius))
    })
    $brush = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255,255,211,83))
    $pen = [Drawing.Pen]::new([Drawing.Color]::FromArgb(255,255,240,173),4)
    $g.FillPolygon($brush,$points)
    $g.DrawPolygon($pen,$points)
    $brush.Dispose(); $pen.Dispose()
}
Draw-Asset 'Sprites/Laser.png' 32 128 {
    param($g)
    $glow = [Drawing.Pen]::new([Drawing.Color]::FromArgb(70,70,230,255),22)
    $core = [Drawing.Pen]::new([Drawing.Color]::FromArgb(255,190,250,255),8)
    $glow.StartCap = $glow.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $core.StartCap = $core.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($glow,16,17,16,111); $g.DrawLine($core,16,17,16,111)
    $glow.Dispose(); $core.Dispose()
}
foreach ($state in @('Normal','Hover','Pressed')) {
    Draw-Asset "UI/Button_$state.png" 512 128 {
        param($g)
        $color = switch ($state) {
            'Hover' { [Drawing.Color]::FromArgb(255,32,112,145) }
            'Pressed' { [Drawing.Color]::FromArgb(255,14,45,70) }
            default { [Drawing.Color]::FromArgb(255,20,65,92) }
        }
        $brush = [Drawing.SolidBrush]::new($color)
        $pen = [Drawing.Pen]::new([Drawing.Color]::FromArgb(255,89,221,244),4)
        $g.FillRectangle($brush,4,4,504,120)
        $g.DrawRectangle($pen,4,4,504,120)
        $brush.Dispose(); $pen.Dispose()
    }
}
Draw-Asset 'UI/Panel.png' 512 512 {
    param($g)
    $brush = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(235,10,23,43))
    $pen = [Drawing.Pen]::new([Drawing.Color]::FromArgb(255,58,141,174),4)
    $g.FillRectangle($brush,4,4,504,504); $g.DrawRectangle($pen,4,4,504,504)
    $brush.Dispose(); $pen.Dispose()
}
