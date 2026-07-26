param(
  [string]$SourceRoot,
  [string]$RuntimePath,
  [string]$ResultsPath,
  [datetime]$NotBeforeUtc,
  [switch]$CheckTestResultsOnly
)

function Assert-FreshPassingTestResults {
  param([string]$Path, [datetime]$NotBefore)
  if (-not (Test-Path -LiteralPath $Path)) { throw "Missing Unity test results: $Path" }
  if ((Get-Item -LiteralPath $Path).LastWriteTimeUtc -le $NotBefore.ToUniversalTime()) { throw "Stale Unity test results: $Path" }
  [xml]$results = Get-Content -Raw -LiteralPath $Path
  $run = $results.'test-run'
  if ($null -eq $run -or $run.result -ne 'Passed' -or [int]$run.failed -ne 0) {
    throw "Unity tests failed: result=$($run.result), failed=$($run.failed)"
  }
}

if ($CheckTestResultsOnly) {
  Assert-FreshPassingTestResults -Path $ResultsPath -NotBefore $NotBeforeUtc
  return
}

if ([string]::IsNullOrWhiteSpace($SourceRoot) -or [string]::IsNullOrWhiteSpace($RuntimePath)) {
  throw 'SourceRoot and RuntimePath are required.'
}

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$source = Join-Path $root $SourceRoot
$runtime = Join-Path $root $RuntimePath
$required = @('manifest.json', 'palette.png', 'flattened.png')
foreach ($file in $required) {
  if (-not (Test-Path -LiteralPath (Join-Path $source $file))) { throw "Missing required source file: $file" }
}
if (-not (Test-Path -LiteralPath $runtime)) { throw "Missing runtime sheet: $RuntimePath" }

Add-Type -AssemblyName System.Drawing
foreach ($path in @((Join-Path $source 'flattened.png'), $runtime)) {
  $image = [Drawing.Image]::FromFile($path)
  try {
    if ($image.Width -ne 256 -or $image.Height -ne 192) { throw "Expected 256 x 192 sheet: $path" }
  } finally { $image.Dispose() }
}

$manifest = Get-Content -Raw -LiteralPath (Join-Path $source 'manifest.json') | ConvertFrom-Json
if ($manifest.cellSize.Count -ne 2 -or $manifest.cellSize[0] -ne 64 -or $manifest.cellSize[1] -ne 64 -or
    $manifest.sheetSize.Count -ne 2 -or $manifest.sheetSize[0] -ne 256 -or $manifest.sheetSize[1] -ne 192 -or
    $manifest.directions.Count -ne 1 -or $manifest.directions[0] -ne 'front' -or
    $manifest.animations.Count -ne 3 -or
    $manifest.animations[0].name -ne 'idle' -or $manifest.animations[0].start -ne 0 -or $manifest.animations[0].frames -ne 2 -or $manifest.animations[0].fps -ne 4 -or
    $manifest.animations[1].name -ne 'move' -or $manifest.animations[1].start -ne 2 -or $manifest.animations[1].frames -ne 4 -or $manifest.animations[1].fps -ne 8 -or
    $manifest.animations[2].name -ne 'death' -or $manifest.animations[2].start -ne 6 -or $manifest.animations[2].frames -ne 6 -or $manifest.animations[2].fps -ne 8) {
  throw 'Invalid front-facing 12-frame metadata.'
}

$uuidPattern = '(?i)\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b'
Get-ChildItem -LiteralPath $source -File -Recurse -Include '*provenance*' | ForEach-Object {
  if (Select-String -LiteralPath $_.FullName -Pattern $uuidPattern -Quiet) { throw "Token-like UUID found in provenance file: $($_.FullName)" }
}

$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe'
$preflightLog = Join-Path $root 'Logs\front-facing-preflight.log'
$validation = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList @('-batchmode', '-nographics', '-quit', '-projectPath', $root, '-executeMethod', 'JoseonHunter.Editor.AssetProduction.FrontFacingCharacterPreflight.ValidateFromCommandLine', '-frontFacingSourceRoot', $SourceRoot, '-frontFacingRuntimePath', $RuntimePath, '-logFile', $preflightLog)
if ($validation.ExitCode -ne 0) { throw "Front-facing contract validation failed. See $preflightLog" }

$testResults = Join-Path $root 'Logs\editmode-results.xml'
$started = [datetime]::UtcNow
$testScript = Join-Path $root 'Tools\Unity\Test-Unity.ps1'
Start-Process -FilePath 'powershell.exe' -Wait -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $testScript, '-Filter', 'JoseonHunter.Tests.EditMode.FrontFacingCharacterSheetContractTests') | Out-Null
$deadline = $started.AddMinutes(3)
do {
  if ((Test-Path -LiteralPath $testResults) -and (Get-Item -LiteralPath $testResults).LastWriteTimeUtc -gt $started) { break }
  Start-Sleep -Seconds 1
} while ([datetime]::UtcNow -lt $deadline)
Assert-FreshPassingTestResults -Path $testResults -NotBefore $started
