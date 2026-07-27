param(
  [Parameter(Mandatory=$true)][string]$AssetId,
  [Parameter(Mandatory=$true)][string]$SourceDirectory,
  [string]$LogFile = ''
)

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe'
if ([string]::IsNullOrWhiteSpace($LogFile)) { $LogFile = Join-Path $root ("Logs\static-sprite-asset-{0}.log" -f $AssetId) }
$arguments = @('-batchmode','-nographics','-projectPath',$root,'-executeMethod','JoseonHunter.Editor.AssetProduction.StaticSpriteBatchContract.ValidateAssetFromCommandLine','-staticSpriteAssetId',$AssetId,'-staticSpriteSourceDirectory',$SourceDirectory,'-logFile',$LogFile)
$process = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList $arguments
if ($process.ExitCode -ne 0) { throw "Static sprite asset validation failed: $AssetId. See $LogFile" }
