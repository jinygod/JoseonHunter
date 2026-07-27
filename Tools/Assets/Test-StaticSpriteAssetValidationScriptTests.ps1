$script = Get-Content -Raw (Join-Path $PSScriptRoot 'Test-StaticSpriteAssetValidation.ps1')
if ($script -match "'-quit'") { throw 'Per-asset route must not pass -quit; ValidateAssetFromCommandLine owns EditorApplication.Exit.' }
foreach ($argument in @('-staticSpriteAssetId','-staticSpriteSourceDirectory')) { if ($script -notmatch [regex]::Escape($argument)) { throw "Missing forwarded argument: $argument" } }
Write-Output 'Static sprite asset validation script arguments PASS'
