param([string]$Filter = 'JoseonHunter.Tests.EditMode')
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$logs = Join-Path $root 'Logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null
& $unity -batchmode -nographics -projectPath $root `
  -runTests -testPlatform editmode -testFilter $Filter `
  -testResults (Join-Path $logs 'editmode-results.xml') `
  -logFile (Join-Path $logs 'editmode.log')
exit $LASTEXITCODE
