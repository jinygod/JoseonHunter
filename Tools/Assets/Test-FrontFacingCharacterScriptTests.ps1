$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$preflight = Join-Path $root 'Tools\Assets\Test-FrontFacingCharacter.ps1'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('front-facing-results-' + [guid]::NewGuid().ToString('N') + '.xml')
try {
  Push-Location $env:TEMP
  try {
    $arguments = & $preflight -SourceRoot 'ArtSource/Pixel/Characters/front-facing/rookie-constable' -RuntimePath 'Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing/rookie_constable.png' -PrintValidationArgumentsOnly
  } finally { Pop-Location }
  if (-not [IO.Path]::IsPathRooted($arguments.SourceRoot) -or -not [IO.Path]::IsPathRooted($arguments.RuntimePath)) {
    throw 'Preflight Unity arguments must be absolute paths.'
  }
  if ($arguments.SourceRoot -ne (Join-Path $root 'ArtSource/Pixel/Characters/front-facing/rookie-constable') -or $arguments.RuntimePath -ne (Join-Path $root 'Assets/JoseonHunter/Art/Characters/Runtime/FrontFacing/rookie_constable.png')) {
    throw 'Preflight Unity arguments were resolved from the caller directory.'
  }

  Set-Content -LiteralPath $fixture -Value '<test-run result="Passed" failed="0" />'
  $stale = (Get-Item -LiteralPath $fixture).LastWriteTimeUtc.AddSeconds(1)
  $staleRejected = $false
  try { & $preflight -CheckTestResultsOnly -ResultsPath $fixture -NotBeforeUtc $stale } catch { $staleRejected = $true }
  if (-not $staleRejected) { throw 'Stale results were accepted.' }

  Set-Content -LiteralPath $fixture -Value '<test-run result="Failed" failed="1" />'
  $failedRejected = $false
  try { & $preflight -CheckTestResultsOnly -ResultsPath $fixture -NotBeforeUtc ([datetime]::UtcNow.AddMinutes(-1)) } catch { $failedRejected = $true }
  if (-not $failedRejected) { throw 'Failed results were accepted.' }
} finally {
  Remove-Item -LiteralPath $fixture -Force -ErrorAction SilentlyContinue
}
