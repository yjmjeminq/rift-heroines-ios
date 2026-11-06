param([int]$Width=2532,[int]$Height=1170,[string]$SafeArea='132,63,2268,1107',[string]$OutputFolder='PhoneSmoke')
$ErrorActionPreference = 'Stop'
if($Width -lt 640 -or $Height -lt 360 -or $OutputFolder -notmatch '^[A-Za-z0-9_-]+$' -or $SafeArea -notmatch '^\d+,\d+,\d+,\d+$'){throw 'Invalid phone test settings'}
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputRoot = Join-Path $projectRoot ('BuildLogs\'+$OutputFolder)
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$log = Join-Path $outputRoot 'player.log'
$arguments = @('-batchmode','-force-d3d11','-screen-width',$Width,'-screen-height',$Height,'-riftMobileQA','-riftMobileSmoke','-riftSafeArea',$SafeArea,'-riftSmokePath',('"'+$outputRoot+'"'),'-logFile',('"'+$log+'"'))
$process = Start-Process -FilePath (Join-Path $projectRoot 'Builds\Windows\RiftHeroines.exe') -ArgumentList $arguments -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(90000)) { Stop-Process -Id $process.Id; throw 'Phone test exceeded 90 seconds.' }
if ($process.ExitCode -ne 0) { throw "Phone UI test failed. See $log" }
Get-Content -LiteralPath (Join-Path $outputRoot 'mobile-report.json')
