param([string]$OutputDirectory = 'Assets/DetectiveDesk/AssetProduction/ClueMemos/20261031_015323_3af426b0')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$rng = New-Object Random 2701
$specs = @(
    @{Name='01_Appointment'; Font='Segoe Script'; Color=[Drawing.Color]::FromArgb(40,53,65); Title='Thursday, after closing'; Lines=@('The lamp was still warm.', 'Someone left in a hurry.', '', 'Ask the night porter', 'who had the spare key.', '', 'Do not telephone.'); Paper=[Drawing.Color]::FromArgb(225,214,183)},
    @{Name='02_Timetable'; Font='Ink Free'; Color=[Drawing.Color]::FromArgb(53,48,42); Title='Check the times'; Lines=@('9:10  -  last tram', '9:25  -  lights upstairs', '9:40  -  rain began', '', 'The witness said nine.', 'Which clock did he use?', '', 'Compare with the ledger.'); Paper=[Drawing.Color]::FromArgb(228,222,202)},
    @{Name='03_Reminder'; Font='Segoe Print'; Color=[Drawing.Color]::FromArgb(57,61,68); Title='Before you leave'; Lines=@('Return the borrowed key.', 'Keep the envelope dry.', '', 'There are two signatures.', 'Only one matches.', '', 'Look beneath the date.'); Paper=[Drawing.Color]::FromArgb(214,216,195)}
)
$sheets = @()
foreach($s in $specs) {
    $b = New-Object Drawing.Bitmap 1024,1024
    for($y=0;$y -lt 1024;$y++) { for($x=0;$x -lt 1024;$x++) {
        $edge = [Math]::Min([Math]::Min($x,1023-$x),[Math]::Min($y,1023-$y))
        $shade = $rng.Next(-5,6) - [int](14*[Math]::Exp(-$edge/30.0)) + [int](2*[Math]::Sin($y/45.0))
        $b.SetPixel($x,$y,[Drawing.Color]::FromArgb([Math]::Max(0,[Math]::Min(255,$s.Paper.R+$shade)),[Math]::Max(0,[Math]::Min(255,$s.Paper.G+$shade)),[Math]::Max(0,[Math]::Min(255,$s.Paper.B+$shade))))
    }}
    $g=[Drawing.Graphics]::FromImage($b); $g.SmoothingMode='AntiAlias'; $g.TextRenderingHint='AntiAliasGridFit'
    $rule=New-Object Drawing.Pen ([Drawing.Color]::FromArgb(35,78,91,105)),1
    for($y=225;$y -lt 940;$y+=80){$g.DrawLine($rule,55,$y,972,$y)}
    $margin=New-Object Drawing.Pen ([Drawing.Color]::FromArgb(45,145,64,50)),2
    $g.DrawLine($margin,102,40,102,982)
    $ink=New-Object Drawing.SolidBrush $s.Color
    $font=New-Object Drawing.Font $s.Font,35,([Drawing.FontStyle]::Regular),([Drawing.GraphicsUnit]::Pixel)
    $title=New-Object Drawing.Font $s.Font,43,([Drawing.FontStyle]::Regular),([Drawing.GraphicsUnit]::Pixel)
    $g.TranslateTransform(123,93); $g.RotateTransform(-1.2); $g.DrawString($s.Title,$title,$ink,0,0); $g.ResetTransform()
    $pen=New-Object Drawing.Pen $s.Color,2
    $g.DrawBezier($pen,125,161,340,168,590,153,863,160)
    for($i=0;$i -lt $s.Lines.Count;$i++) {
        $g.TranslateTransform(124+$rng.Next(-5,6),200+$i*80); $g.RotateTransform([single]($rng.NextDouble()*1.6-0.8))
        $g.DrawString($s.Lines[$i],$font,$ink,0,0); $g.ResetTransform()
    }
    $fold=New-Object Drawing.Pen ([Drawing.Color]::FromArgb(18,92,71,43)),3
    $g.DrawLine($fold,20,518,1000,511)
    $b.Save((Join-Path $OutputDirectory ($s.Name+'.png')),[Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose();$font.Dispose();$title.Dispose();$ink.Dispose();$pen.Dispose();$rule.Dispose();$margin.Dispose();$fold.Dispose()
    $sheets += $b
}
$contact=New-Object Drawing.Bitmap 2048,2048
$g=[Drawing.Graphics]::FromImage($contact); $g.Clear([Drawing.Color]::FromArgb(34,32,29))
for($i=0;$i -lt 3;$i++){ $x=80+($i%2)*990; $y=70+[Math]::Floor($i/2)*1000; $g.DrawImage($sheets[$i],[int]$x,[int]$y,900,900) }
$g.DrawString('CLUE MEMOS / 3 loose leaves',(New-Object Drawing.Font 'Segoe Print',38),[Drawing.Brushes]::Wheat,1090,1180)
$g.DrawString("Provisional case notes`nHandwriting / paper study`nNarrative approval pending",(New-Object Drawing.Font 'Segoe Print',25),[Drawing.Brushes]::Tan,1090,1280)
$contact.Save((Join-Path $OutputDirectory 'ContactSheet.png'),[Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$contact.Dispose(); foreach($b in $sheets){$b.Dispose()}
Write-Output 'Generated three separate 1024-square memo textures and 2048-square contact sheet.'

