param(
  [Parameter(Mandatory=$true)][string]$ManifestPath,
  [Parameter(Mandatory=$true)][string]$SourceRoot,
  [string]$RuntimeRoot = '',
  [switch]$RequireRuntime,
  [switch]$PrintValidationArgumentsOnly
)

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
function Resolve-ProjectPath([string]$path) { if ([IO.Path]::IsPathRooted($path)) { return [IO.Path]::GetFullPath($path) }; return [IO.Path]::GetFullPath((Join-Path $root $path)) }
$manifest = Resolve-ProjectPath $ManifestPath
$source = Resolve-ProjectPath $SourceRoot
$runtime = if ([string]::IsNullOrWhiteSpace($RuntimeRoot)) { '' } else { Resolve-ProjectPath $RuntimeRoot }
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) { throw "Missing manifest: $manifest" }
if (-not (Test-Path -LiteralPath $source -PathType Container)) { throw "Missing source root: $source" }
if ($RequireRuntime -and (-not (Test-Path -LiteralPath $runtime -PathType Container))) { throw "Missing runtime root: $runtime" }
if ($PrintValidationArgumentsOnly) { [pscustomobject]@{ ManifestPath = $manifest; SourceRoot = $source; RuntimeRoot = $runtime; RequireRuntime = [bool]$RequireRuntime }; return }
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe'
$log = Join-Path $root 'Logs\static-sprite-batch-preflight.log'
$arguments = @('-batchmode','-nographics','-quit','-projectPath',$root,'-executeMethod','JoseonHunter.Editor.AssetProduction.StaticSpriteBatchContract.ValidateFromCommandLine','-staticSpriteManifestPath',$manifest,'-staticSpriteSourceRoot',$source,'-staticSpriteRuntimeRoot',$runtime,'-logFile',$log)
if ($RequireRuntime) { $arguments += '-staticSpriteRequireRuntime' }
$process = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList $arguments
if ($process.ExitCode -ne 0) { throw "Static sprite batch validation failed. See $log" }
