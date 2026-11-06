param([int]$Width=1600,[int]$Height=900,[string]$OutputFolder='PlayerSmoke')
$ErrorActionPreference = 'Stop'
if($Width -lt 640 -or $Height -lt 360 -or $OutputFolder -notmatch '^[A-Za-z0-9_-]+$'){throw 'Invalid smoke dimensions or output folder'}
$projectRoot = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $projectRoot 'Builds\Windows\RiftHeroines.exe'
$outputRoot = Join-Path $projectRoot ('BuildLogs\'+$OutputFolder)
$log = Join-Path $projectRoot ('BuildLogs\player-smoke-'+$OutputFolder+'.log')
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$arguments = @('-batchmode','-force-d3d11','-screen-width',$Width,'-screen-height',$Height,'-riftSmoke','-riftSmokePath',('"'+$outputRoot+'"'),'-logFile',('"'+$log+'"'))
$process = Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(180000)) { throw 'Player smoke exceeded 180 seconds.' }
if ($process.ExitCode -ne 0) { throw "Player smoke failed. See $log" }
Get-Content -LiteralPath (Join-Path $outputRoot 'report.json')
