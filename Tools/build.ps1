param([switch]$ValidateOnly)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$editor = 'C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe'
if (-not (Test-Path -LiteralPath $editor)) { throw 'Unity 6000.3.27f1 is required.' }
New-Item -ItemType Directory -Force -Path (Join-Path $projectRoot 'BuildLogs') | Out-Null
$method = if ($ValidateOnly) { 'RiftHeroines.EditorTools.RiftBuilder.ValidateOnly' } else { 'RiftHeroines.EditorTools.RiftBuilder.PrepareAndBuild' }
$log = Join-Path $projectRoot 'BuildLogs\unity-build.log'
$arguments = @('-batchmode','-quit','-projectPath',('"'+$projectRoot+'"'),'-executeMethod',$method,'-logFile',('"'+$log+'"'))
$process = Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity failed ($($process.ExitCode)). See $log" }
$report = Get-Content -LiteralPath (Join-Path $projectRoot 'BuildLogs\model-validation.json') -Raw | ConvertFrom-Json
[pscustomobject]@{ passed = $report.passed; checks = $report.checks.Count; campaigns = $report.campaigns.Count }
