$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$preflight = Join-Path $root 'Tools\Assets\Test-FrontFacingCharacter.ps1'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('front-facing-results-' + [guid]::NewGuid().ToString('N') + '.xml')
try {
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
