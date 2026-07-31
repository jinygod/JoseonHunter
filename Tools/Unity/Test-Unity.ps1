param(
    [ValidateSet('editmode', 'playmode')]
    [string]$Platform = 'editmode',
    [string]$Filter = 'JoseonHunter.Tests.EditMode'
)

$unity = 'C:\\Program Files\\Unity\\Hub\\Editor\\6000.5.5f1\\Editor\\Unity.exe'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$logs = Join-Path $root 'Logs'
$results = Join-Path $logs ($Platform + '-results.xml')
$log = Join-Path $logs ($Platform + '.log')
New-Item -ItemType Directory -Path $logs -Force | Out-Null
if (Test-Path -LiteralPath $results) { Remove-Item -LiteralPath $results -Force }

$arguments = @(
    '-batchmode', '-nographics', '-projectPath', $root,
    '-runTests', '-testPlatform', $Platform, '-testFilter', $Filter,
    '-testResults', $results, '-logFile', $log
)
$process = Start-Process -FilePath $unity -ArgumentList $arguments `
    -Wait -PassThru -WindowStyle Hidden
if (-not (Test-Path -LiteralPath $results)) {
    throw "Unity did not produce $results. Inspect $log."
}
exit $process.ExitCode
