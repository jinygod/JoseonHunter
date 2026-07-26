[CmdletBinding()]
param(
  [Parameter(Mandatory)][string]$SourceRoot,
  [Parameter(Mandatory)][string]$UnityRoot,
  [Parameter(Mandatory)][string]$ManifestPath,
  [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

function Resolve-ChildPath {
  param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$RelativePath)
  $rootFullPath = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
  $candidatePath = [IO.Path]::GetFullPath((Join-Path $rootFullPath $RelativePath))
  $rootPrefix = $rootFullPath + [IO.Path]::DirectorySeparatorChar
  if (-not $candidatePath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Path escapes root: $RelativePath" }
  return $candidatePath
}

function Test-AllowedDestination {
  param([Parameter(Mandatory)][string]$UnityRootPath, [Parameter(Mandatory)][string]$DestinationPath)
  $assetRoot = [IO.Path]::GetFullPath((Join-Path $UnityRootPath 'Assets/JoseonHunter'))
  $docsRoot = [IO.Path]::GetFullPath((Join-Path $UnityRootPath 'Docs/Assets'))
  foreach ($allowedRoot in @($assetRoot, $docsRoot)) {
    $allowedPrefix = $allowedRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if ($DestinationPath.StartsWith($allowedPrefix, [StringComparison]::OrdinalIgnoreCase)) { return }
  }
  throw "Destination is outside allowed roots: $DestinationPath"
}

$failed = $false
try {
  $sourceRootPath = [IO.Path]::GetFullPath($SourceRoot)
  $unityRootPath = [IO.Path]::GetFullPath($UnityRoot)
  if (-not (Test-Path -LiteralPath $sourceRootPath -PathType Container)) { throw "Source root does not exist: $sourceRootPath" }
  if (-not (Test-Path -LiteralPath $unityRootPath -PathType Container)) { throw "Unity root does not exist: $unityRootPath" }
  if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) { throw "Manifest does not exist: $ManifestPath" }
  $manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
  if ($null -eq $manifest.entries) { throw 'Manifest has no entries.' }
}
catch {
  Write-Error $_.Exception.Message
  exit 1
}

foreach ($entry in @($manifest.entries)) {
  $result = [ordered]@{ source = $entry.source; destination = $entry.destination; profile = $entry.profile; hash = $null; action = 'failed' }
  try {
    if ($entry.licenseStatus -cne 'approved') { throw "License is not approved: $($entry.licenseStatus)" }
    if ([string]::IsNullOrWhiteSpace($entry.source) -or [string]::IsNullOrWhiteSpace($entry.destination)) { throw 'Manifest entry requires source and destination.' }
    $sourcePath = Resolve-ChildPath -Root $sourceRootPath -RelativePath $entry.source
    $destinationPath = Resolve-ChildPath -Root $unityRootPath -RelativePath $entry.destination
    Test-AllowedDestination -UnityRootPath $unityRootPath -DestinationPath $destinationPath
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) { throw "Source does not exist: $sourcePath" }
    $sourceHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
    $result.hash = $sourceHash
    if ((Test-Path -LiteralPath $destinationPath -PathType Leaf) -and $sourceHash -eq (Get-FileHash -LiteralPath $destinationPath -Algorithm SHA256).Hash) { $result.action = 'unchanged' }
    elseif ($DryRun) { $result.action = 'would-copy' }
    else {
      New-Item -ItemType Directory -Path (Split-Path -Parent $destinationPath) -Force | Out-Null
      Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Force
      $result.action = 'copied'
    }
  }
  catch {
    $failed = $true
    $result.error = $_.Exception.Message
  }
  [pscustomobject]$result | ConvertTo-Json -Compress
}

if ($failed) { exit 1 }
exit 0
