param(
  [string]$ManifestPath,
  [ValidateSet('characters', 'enemies', 'weapons_vfx', 'stage', 'ui', 'audio', 'store')]
  [string]$Batch,
  [switch]$SkipUnity
)

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
  $ManifestPath = Join-Path $root 'Docs\Assets\production-asset-manifest.json'
}
$manifest = Get-Content -Raw -LiteralPath $ManifestPath | ConvertFrom-Json
$errors = [Collections.Generic.List[string]]::new()
$selected = if ($Batch) { @($manifest.assets | Where-Object batch -eq $Batch) } else { @($manifest.assets) }
foreach ($asset in $selected) {
  $source = Join-Path $root $asset.sourcePath
  if (-not (Test-Path -LiteralPath $source)) { $errors.Add("missing source: $($asset.id)") }
  else {
    try {
      $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $source).Hash.ToLowerInvariant()
      if ($asset.sha256 -and $asset.sha256.ToLowerInvariant() -ne $hash) {
        $errors.Add("SHA-256 mismatch: $($asset.id)")
      }
      if ([IO.Path]::GetExtension($source) -ieq '.png') {
        Add-Type -AssemblyName System.Drawing
        $image = [Drawing.Image]::FromFile($source)
        try {
          if ($asset.width -and $image.Width -ne [int]$asset.width) { $errors.Add("width mismatch: $($asset.id)") }
          if ($asset.height -and $image.Height -ne [int]$asset.height) { $errors.Add("height mismatch: $($asset.id)") }
        } finally { $image.Dispose() }
      }
    } catch { $errors.Add("malformed source: $($asset.id)") }
  }
}
if ($errors.Count -gt 0) { $errors | Write-Error; exit 1 }
if ($SkipUnity) { exit 0 }
if (@($selected | Where-Object { $_.id -eq 'mannequin_runtime' }).Count -gt 0) {
  & "$root\Tools\Unity\Test-Unity.ps1" -Filter JoseonHunter.Tests.EditMode.CharacterSheetContractTests
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
& "$root\Tools\Unity\Test-Unity.ps1" -Filter JoseonHunter.Tests.EditMode.ProductionAsset
exit $LASTEXITCODE
