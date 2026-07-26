$ErrorActionPreference = 'Stop'

function Invoke-Sync {
  param(
    [string]$SourceRoot,
    [string]$UnityRoot,
    [string]$ManifestPath,
    [switch]$DryRun
  )

  $arguments = @(
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', (Join-Path $PSScriptRoot 'Sync-FlutterAssets.ps1'),
    '-SourceRoot', $SourceRoot,
    '-UnityRoot', $UnityRoot,
    '-ManifestPath', $ManifestPath
  )
  if ($DryRun) { $arguments += '-DryRun' }

  & powershell @arguments | Write-Host
  return $LASTEXITCODE
}

$sandbox = Join-Path ([IO.Path]::GetTempPath()) ('joseon-assets-' + [guid]::NewGuid())
try {
  $source = Join-Path $sandbox 'source'
  $unity = Join-Path $sandbox 'unity'
  New-Item -ItemType Directory -Path (Join-Path $source 'assets\images') -Force | Out-Null
  New-Item -ItemType Directory -Path $unity -Force | Out-Null
  Set-Content -LiteralPath (Join-Path $source 'assets\images\hero.png') -Value 'fixture'

  $manifest = @{
    version = 1
    entries = @(@{
      source = 'assets/images/hero.png'
      destination = 'Assets/JoseonHunter/Art/Characters/hero.png'
      profile = 'pixel'
      licenseStatus = 'approved'
    })
  } | ConvertTo-Json -Depth 5
  $manifestPath = Join-Path $sandbox 'manifest.json'
  Set-Content -LiteralPath $manifestPath -Value $manifest -Encoding UTF8

  $exitCode = Invoke-Sync -SourceRoot $source -UnityRoot $unity -ManifestPath $manifestPath
  if ($exitCode -ne 0) { throw "sync failed: $exitCode" }

  $copied = Join-Path $unity 'Assets\JoseonHunter\Art\Characters\hero.png'
  if (-not (Test-Path -LiteralPath $copied)) { throw 'approved asset was not copied' }
  if ((Get-Content -LiteralPath $copied -Raw) -ne "fixture`r`n") { throw 'copied asset contents differ' }

  $bad = $manifest -replace '"approved"', '"unresolved"'
  Set-Content -LiteralPath $manifestPath -Value $bad -Encoding UTF8
  $exitCode = Invoke-Sync -SourceRoot $source -UnityRoot $unity -ManifestPath $manifestPath
  if ($exitCode -eq 0) { throw 'unresolved license did not block sync' }

  $traversal = $manifest -replace 'Assets/JoseonHunter/Art/Characters/hero.png', 'Assets/JoseonHunter/../outside.png'
  Set-Content -LiteralPath $manifestPath -Value $traversal -Encoding UTF8
  $exitCode = Invoke-Sync -SourceRoot $source -UnityRoot $unity -ManifestPath $manifestPath
  if ($exitCode -eq 0) { throw 'destination traversal did not block sync' }

  Write-Host 'PASS: approved assets copy; unresolved licenses and path traversal block sync.'
}
finally {
  if (Test-Path -LiteralPath $sandbox) {
    Remove-Item -LiteralPath $sandbox -Recurse -Force
  }
}
