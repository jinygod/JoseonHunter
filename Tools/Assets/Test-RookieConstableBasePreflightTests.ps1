$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$preflight = Join-Path $root 'Tools\Assets\Test-RookieConstableBasePreflight.ps1'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('rookie-constable-preflight-' + [guid]::NewGuid().ToString('N') + '.png')

function Write-Fixture([int]$X, [int]$Y) {
  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::new(64, 64, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
  try {
    $bitmap.SetPixel($X, $Y, [Drawing.Color]::Black)
    $bitmap.Save($fixture, [Drawing.Imaging.ImageFormat]::Png)
  } finally { $bitmap.Dispose() }
}

try {
  Write-Fixture -X 32 -Y 56
  $result = & $preflight -Path $fixture | ConvertFrom-Json
  if ($result.Result -ne 'PASS') { throw 'Valid fixture did not pass.' }

  Write-Fixture -X 32 -Y 57
  $bottomRejected = $false
  try { & $preflight -Path $fixture | Out-Null } catch { $bottomRejected = $true }
  if (-not $bottomRejected) { throw 'Fixture with maxY 57 was accepted.' }

  Write-Fixture -X 0 -Y 56
  $centerRejected = $false
  try { & $preflight -Path $fixture | Out-Null } catch { $centerRejected = $true }
  if (-not $centerRejected) { throw 'Fixture outside horizontal center tolerance was accepted.' }
} finally {
  Remove-Item -LiteralPath $fixture -Force -ErrorAction SilentlyContinue
}
