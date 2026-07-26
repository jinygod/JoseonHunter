param(
  [string]$ManifestPath =
    'D:\UnityProjects\JoseonHunter\Docs\Assets\production-asset-manifest.json',
  [ValidateSet('characters', 'enemies', 'weapons_vfx', 'stage', 'ui', 'audio', 'store')]
  [string]$Batch
)

$root = 'D:\UnityProjects\JoseonHunter'
$manifest = Get-Content -Raw -LiteralPath $ManifestPath | ConvertFrom-Json
$errors = [Collections.Generic.List[string]]::new()
$selected = if ($Batch) { @($manifest.assets | Where-Object batch -eq $Batch) } else { @($manifest.assets) }
foreach ($asset in $selected) {
  $source = Join-Path $root $asset.sourcePath
  if (-not (Test-Path -LiteralPath $source)) { $errors.Add("missing source: $($asset.id)") }
  else { Get-FileHash -Algorithm SHA256 -LiteralPath $source | Out-Null }
}
if ($errors.Count -gt 0) { $errors | Write-Error; exit 1 }
& "$root\Tools\Unity\Test-Unity.ps1" -Filter JoseonHunter.Tests.EditMode.ProductionAsset
exit $LASTEXITCODE
