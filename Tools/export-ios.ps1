param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$editor = 'C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe'
if (-not (Test-Path -LiteralPath $editor)) { throw 'Unity 6000.3.27f1 with iOS Build Support is required.' }
New-Item -ItemType Directory -Force -Path (Join-Path $projectRoot 'BuildLogs') | Out-Null
$log = Join-Path $projectRoot 'BuildLogs\unity-ios-export.log'
$arguments = @('-batchmode','-quit','-buildTarget','iOS','-projectPath',('"'+$projectRoot+'"'),'-executeMethod','RiftHeroines.EditorTools.RiftBuilder.ExportIOS','-logFile',('"'+$log+'"'))
$process = Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity iOS export failed. See $log" }
Get-Content -LiteralPath (Join-Path $projectRoot 'BuildLogs\build-ios.json')
