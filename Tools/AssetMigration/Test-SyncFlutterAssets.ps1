$ErrorActionPreference = 'Stop'

function New-Manifest {
  param([object[]]$Entries)
  return (@{ version = 1; entries = $Entries } | ConvertTo-Json -Depth 5)
}

function Set-TestManifest {
  param([string]$ManifestPath, [object[]]$Entries)
  Set-Content -LiteralPath $ManifestPath -Value (New-Manifest $Entries) -Encoding UTF8
}

function New-Entry {
  param([string]$Source, [string]$Destination, [string]$LicenseStatus = 'approved')
  return @{ source = $Source; destination = $Destination; profile = 'pixel'; licenseStatus = $LicenseStatus }
}

function Invoke-Sync {
  param(
    [string]$SourceRoot,
    [string]$UnityRoot,
    [string]$ManifestPath,
    [switch]$DryRun,
    [string[]]$AdditionalArguments = @()
  )

  $arguments = @('-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $PSScriptRoot 'Sync-FlutterAssets.ps1'))
  if ($SourceRoot) { $arguments += @('-SourceRoot', $SourceRoot) }
  if ($UnityRoot) { $arguments += @('-UnityRoot', $UnityRoot) }
  if ($ManifestPath) { $arguments += @('-ManifestPath', $ManifestPath) }
  if ($DryRun) { $arguments += '-DryRun' }
  $arguments += $AdditionalArguments

  $previousErrorAction = $ErrorActionPreference
  try {
    $ErrorActionPreference = 'Continue'
    $output = & powershell @arguments 2>&1 | ForEach-Object { $_.ToString() }
    $exitCode = $LASTEXITCODE
  }
  finally {
    $ErrorActionPreference = $previousErrorAction
  }
  return [pscustomobject]@{ ExitCode = $exitCode; Output = @($output) }
}

function Get-JsonResults {
  param([object]$Invocation)
  $lines = @($Invocation.Output | Where-Object { $_.TrimStart().StartsWith('{') })
  return ,@($lines | ForEach-Object { $_ | ConvertFrom-Json })
}

function Assert-True {
  param([bool]$Condition, [string]$Message)
  if (-not $Condition) { throw $Message }
}

$sandbox = Join-Path ([IO.Path]::GetTempPath()) ('joseon-assets-' + [guid]::NewGuid())
try {
  $source = Join-Path $sandbox 'source'
  $unity = Join-Path $sandbox 'unity'
  $outsideSource = Join-Path $sandbox 'sourceElsewhere'
  New-Item -ItemType Directory -Path (Join-Path $source 'assets\images'), $unity, $outsideSource -Force | Out-Null
  Set-Content -LiteralPath (Join-Path $source 'assets\images\hero.png') -Value 'fixture'
  Set-Content -LiteralPath (Join-Path $outsideSource 'outside.png') -Value 'outside'
  $manifestPath = Join-Path $sandbox 'manifest.json'
  $approved = New-Entry -Source 'assets/images/hero.png' -Destination 'Assets/JoseonHunter/Art/Characters/hero.png'

  # A missing required parameter must not silently select a default source root.
  Set-TestManifest -ManifestPath $manifestPath -Entries @($approved)
  $required = Invoke-Sync -UnityRoot $unity -ManifestPath $manifestPath
  Assert-True ($required.ExitCode -ne 0) 'missing mandatory SourceRoot did not fail'

  # An approved asset must copy once, produce a parseable report, then be hash-idempotent.
  $copy = Invoke-Sync -SourceRoot $source -UnityRoot $unity -ManifestPath $manifestPath
  Assert-True ($copy.ExitCode -eq 0) "approved asset sync failed: $($copy.Output -join [Environment]::NewLine)"
  $copyResults = Get-JsonResults $copy
  Assert-True ($copyResults.Count -eq 1) "approved sync did not produce one JSON report object: $($copy.Output -join '|')"
  foreach ($field in @('source', 'destination', 'profile', 'hash', 'action')) { Assert-True ($null -ne $copyResults[0].PSObject.Properties[$field]) "JSON report omitted $field" }
  Assert-True ($copyResults[0].action -eq 'copied') 'approved asset was not reported as copied'
  $copied = Join-Path $unity 'Assets\JoseonHunter\Art\Characters\hero.png'
  Assert-True (Test-Path -LiteralPath $copied) 'approved asset was not copied'
  Assert-True ((Get-Content -LiteralPath $copied -Raw) -eq "fixture`r`n") 'copied asset contents differ'
  $unchanged = Invoke-Sync -SourceRoot $source -UnityRoot $unity -ManifestPath $manifestPath
  Assert-True ($unchanged.ExitCode -eq 0) 'unchanged asset sync failed'
  Assert-True ((Get-JsonResults $unchanged)[0].action -eq 'unchanged') 'matching SHA-256 did not report unchanged'

  # Dry run must report the copy without creating either the destination file or its directory.
  $dryDestination = 'Assets/JoseonHunter/Art/DryRun/nested/hero.png'
  Set-TestManifest -ManifestPath $manifestPath -Entries @(New-Entry -Source 'assets/images/hero.png' -Destination $dryDestination)
  $dryRun = Invoke-Sync -SourceRoot $source -UnityRoot $unity -ManifestPath $manifestPath -DryRun
  Assert-True ($dryRun.ExitCode -eq 0) 'dry run failed'
  Assert-True ((Get-JsonResults $dryRun)[0].action -eq 'would-copy') 'dry run did not report would-copy'
  Assert-True (-not (Test-Path -LiteralPath (Join-Path $unity 'Assets\JoseonHunter\Art\DryRun'))) 'dry run created a destination directory'

  # Both source and destination prefix-boundary escapes must fail.
  Set-TestManifest -ManifestPath $manifestPath -Entries @(New-Entry -Source '../sourceElsewhere/outside.png' -Destination 'Assets/JoseonHunter/Art/Characters/outside.png')
  $sourceTraversal = Invoke-Sync -SourceRoot $source -UnityRoot $unity -ManifestPath $manifestPath
  Assert-True ($sourceTraversal.ExitCode -ne 0) 'source traversal did not block sync'
  Set-TestManifest -ManifestPath $manifestPath -Entries @(New-Entry -Source 'assets/images/hero.png' -Destination 'Assets/JoseonHunterElsewhere/outside.png')
  $destinationPrefix = Invoke-Sync -SourceRoot $source -UnityRoot $unity -ManifestPath $manifestPath
  Assert-True ($destinationPrefix.ExitCode -ne 0) 'destination prefix-boundary escape did not block sync'

  # A bad entry must not stop later processing, and the aggregate exit status must fail.
  Set-TestManifest -ManifestPath $manifestPath -Entries @(
    (New-Entry -Source 'assets/images/missing.png' -Destination 'Assets/JoseonHunter/Art/Characters/missing.png'),
    (New-Entry -Source 'assets/images/hero.png' -Destination 'Assets/JoseonHunter/Art/Characters/after-missing.png')
  )
  $aggregate = Invoke-Sync -SourceRoot $source -UnityRoot $unity -ManifestPath $manifestPath
  Assert-True ($aggregate.ExitCode -eq 1) 'missing source did not set aggregate exit code to 1'
  $aggregateResults = Get-JsonResults $aggregate
  Assert-True ($aggregateResults.Count -eq 2) 'missing source did not report every manifest entry'
  Assert-True ($aggregateResults[0].action -eq 'failed') 'missing source was not reported as failed'
  Assert-True ($aggregateResults[1].action -eq 'copied') 'entry after missing source was not processed'
  Assert-True (Test-Path -LiteralPath (Join-Path $unity 'Assets\JoseonHunter\Art\Characters\after-missing.png')) 'valid entry after missing source was not copied'

  # Unapproved licenses remain blocking after the broader contract coverage.
  Set-TestManifest -ManifestPath $manifestPath -Entries @(New-Entry -Source 'assets/images/hero.png' -Destination 'Assets/JoseonHunter/Art/Characters/unapproved.png' -LicenseStatus 'unresolved')
  $unapproved = Invoke-Sync -SourceRoot $source -UnityRoot $unity -ManifestPath $manifestPath
  Assert-True ($unapproved.ExitCode -ne 0) 'unresolved license did not block sync'

  Write-Host 'PASS: sync rejects invalid inputs, preserves dry-run safety, aggregates failures, reports JSON, and is SHA-idempotent.'
}
finally {
  if (Test-Path -LiteralPath $sandbox) { Remove-Item -LiteralPath $sandbox -Recurse -Force }
}
