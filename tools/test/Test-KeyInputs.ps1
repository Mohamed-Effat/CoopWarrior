param([int]$slot = 0)
$sp = Split-Path -Parent $MyInvocation.MyCommand.Path
. "$sp\Load-PadTest.ps1"
function Probe($label, [byte[]]$keys, [int[]]$mouse = @()) {
  foreach ($k in $keys) { [T]::KeyDown($k) }
  foreach ($m in $mouse) { [T]::MouseDown($m) }
  Start-Sleep -Milliseconds 120
  $line = [T]::Pads().Split("`n")[$slot].Trim()
  foreach ($m in $mouse) { [T]::MouseUp($m) }
  foreach ($k in $keys) { [T]::KeyUp($k) }
  Start-Sleep -Milliseconds 80
  "{0,-22} {1}" -f $label, $line
}
Probe 'W (up)'          @(0x57)
Probe 'S (down)'        @(0x53)
Probe 'A (left)'        @(0x41)
Probe 'D (right)'       @(0x44)
Probe 'W+D (diag)'      @(0x57, 0x44)
Probe 'S+A (diag)'      @(0x53, 0x41)
Probe 'W+S (cancel)'    @(0x57, 0x53)
Probe 'W+A+D'           @(0x57, 0x41, 0x44)
Probe 'Up arrow'        @(0x26)
Probe 'Left arrow'      @(0x25)
Probe 'Up+Right arrows' @(0x26, 0x27)
Probe 'LShift (LT)'     @(0xA0)
Probe 'RShift (LT)'     @(0xA1)
Probe 'LCtrl (RT)'      @(0xA2)
Probe 'RCtrl (RT)'      @(0xA3)
Probe 'T (dpad up)'     @(0x54)
Probe 'G (dpad down)'   @(0x47)
Probe 'F (dpad left)'   @(0x46)
Probe 'H (dpad right)'  @(0x48)
Probe 'T+H (dpad diag)' @(0x54, 0x48)
Probe 'Space (A)'       @(0x20)
Probe 'L (B)'           @(0x4C)
Probe 'J (X)'           @(0x4A)
Probe 'K (Y)'           @(0x4B)
Probe 'Q (LB)'          @(0x51)
Probe 'E (RB)'          @(0x45)
Probe 'Z (L3)'          @(0x5A)
Probe 'X (R3)'          @(0x58)
Probe 'Return (Start)'  @(0x0D)
Probe 'Tab (Back)'      @(0x09)
Probe 'Mouse mid (B)'   @() @(4)
Probe 'Nothing'         @()
