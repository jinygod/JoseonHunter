$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$script = Join-Path $root 'Tools\Assets\Test-StaticSpriteBatch.ps1'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('static sprite batch ' + [guid]::NewGuid().ToString('N'))
try {
  New-Item -ItemType Directory -Path $temp | Out-Null
  $manifest = Join-Path $temp 'batch manifest.json'; Set-Content -LiteralPath $manifest -Value '{}'
  $source = Join-Path $temp 'source root'; New-Item -ItemType Directory -Path $source | Out-Null
  $runtime = Join-Path $temp 'runtime root'; New-Item -ItemType Directory -Path $runtime | Out-Null
  Push-Location $env:TEMP
  try { $arguments = & $script -ManifestPath $manifest -SourceRoot $source -RuntimeRoot $runtime -RequireRuntime -PrintValidationArgumentsOnly } finally { Pop-Location }
  if ($arguments.ManifestPath -ne [IO.Path]::GetFullPath($manifest) -or $arguments.SourceRoot -ne [IO.Path]::GetFullPath($source) -or $arguments.RuntimeRoot -ne [IO.Path]::GetFullPath($runtime)) { throw 'Paths containing spaces were not resolved exactly.' }
  if (-not $arguments.RequireRuntime) { throw '-RequireRuntime was not forwarded.' }
  $missingRejected = $false; try { & $script -ManifestPath (Join-Path $temp 'missing.json') -SourceRoot $source -PrintValidationArgumentsOnly:$false } catch { $missingRejected = $true }
  if (-not $missingRejected) { throw 'Missing manifest was accepted.' }
} finally { Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue }
