$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not ('T' -as [type])) {
  Add-Type -Path (Join-Path $here 'PadTest.cs') -ReferencedAssemblies System.Drawing
}
[T]::Init()
$S = $here
