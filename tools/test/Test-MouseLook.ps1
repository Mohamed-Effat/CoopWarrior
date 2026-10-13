$sp = Split-Path -Parent $MyInvocation.MyCommand.Path
. "$sp\Load-PadTest.ps1"
function Burst([int]$dx, [int]$dy, [int]$steps, [string]$label) {
  $mid = $null
  for ($i = 0; $i -lt $steps; $i++) {
    [T]::MoveRel($dx, $dy)
    Start-Sleep -Milliseconds 5
    if ($i -eq [int]($steps * 0.7)) { $mid = [T]::Pads().Split("`n")[0].Trim() }
  }
  Start-Sleep -Milliseconds 120
  $after = [T]::Pads().Split("`n")[0].Trim()
  "{0,-22} during: {1}" -f $label, $mid
  "{0,-22} after:  {1}" -f '', $after
}
Burst 2 0 40 'slow right'
Burst 12 0 40 'fast right'
Burst -12 0 40 'fast left'
Burst 0 -8 40 'up'
Burst 0 8 40 'down'
