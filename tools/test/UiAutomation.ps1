Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
function Get-AppRoot {
  $p = Get-Process CoopWarrior -ErrorAction Stop | Select-Object -First 1
  $cond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ProcessIdProperty), $p.Id
  [System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
}
function Find-UI([string]$Name, [int]$Index = 0) {
  $root = Get-AppRoot
  $cond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty), $Name
  $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
  if ($all.Count -le $Index) { throw "UI element '$Name' not found" }
  $all[$Index]
}
function Invoke-UI([string]$Name, [int]$Index = 0) {
  $root = Get-AppRoot
  $cond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty), $Name
  $n = 0
  foreach ($e in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)) {
    $pat = $null
    $kind = $null
    if ($e.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pat)) { $kind = 'invoke' }
    elseif ($e.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$pat)) { $kind = 'select' }
    elseif ($e.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$pat)) { $kind = 'toggle' }
    if (-not $kind) { continue }
    if ($n++ -lt $Index) { continue }
    switch ($kind) { 'invoke' { $pat.Invoke() } 'select' { $pat.Select() } 'toggle' { $pat.Toggle() } }
    return
  }
  throw "No invokable element '$Name'"
}
function Center-UI([string]$Name, [int]$Index = 0) {
  $r = (Find-UI $Name $Index).Current.BoundingRectangle
  @([int]($r.X + $r.Width / 2), [int]($r.Y + $r.Height / 2))
}
