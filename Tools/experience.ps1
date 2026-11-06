param([string]$OutputFolder='Experience',[int]$Width=1280,[int]$Height=720)
$ErrorActionPreference='Stop'
if($OutputFolder -notmatch '^[A-Za-z0-9_-]+$'){throw 'Invalid experience output folder'}
$projectRoot=Split-Path -Parent $PSScriptRoot
$outputRoot=Join-Path $projectRoot ('BuildLogs\'+$OutputFolder)
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$exe=Join-Path $projectRoot 'Builds\Windows\RiftHeroines.exe'
$log=Join-Path $outputRoot 'player.log'
$arguments=@('-batchmode','-force-d3d11','-screen-width',$Width,'-screen-height',$Height,'-riftExperience','-riftSmokePath',('"'+$outputRoot+'"'),'-logFile',('"'+$log+'"'))
$process=Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
if(-not $process.WaitForExit(120000)){throw 'Experience observation timed out'}
if($process.ExitCode -ne 0){throw 'Experience observation failed; inspect its report and log'}
Get-Content -LiteralPath (Join-Path $outputRoot 'report.json') -Raw | ConvertFrom-Json | Select-Object passed,version,realSeconds,simulationSeconds,hitEvents,inputs,errors
