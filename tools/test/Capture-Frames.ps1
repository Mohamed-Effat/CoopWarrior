param([string]$Out, [int]$Count = 8, [int]$IntervalMs = 700, [double]$Scale = 0.33, [int]$Cols = 4)
$sp = Split-Path -Parent $MyInvocation.MyCommand.Path
. "$sp\Load-PadTest.ps1"
Add-Type -AssemblyName System.Drawing
$hwnd = [T]::Win('CoopWarrior')
$first = [T]::Capture($hwnd)
$fw = [int]($first.Width * $Scale); $fh = [int]($first.Height * $Scale)
$first.Dispose()
$rows = [int][math]::Ceiling($Count / $Cols)
$sheet = New-Object System.Drawing.Bitmap ([int]($fw * $Cols)), ([int]($fh * $rows))
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.InterpolationMode = 'HighQualityBicubic'
for ($i = 0; $i -lt $Count; $i++) {
  $frame = [T]::Capture($hwnd)
  $g.DrawImage($frame, ($i % $Cols) * $fw, [math]::Floor($i / $Cols) * $fh, $fw, $fh)
  $g.DrawString("$($i * $IntervalMs)ms", (New-Object System.Drawing.Font 'Segoe UI', 10), [System.Drawing.Brushes]::Yellow, ($i % $Cols) * $fw + 4, [math]::Floor($i / $Cols) * $fh + 2)
  $frame.Dispose()
  Start-Sleep -Milliseconds $IntervalMs
}
$g.Dispose()
$sheet.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
