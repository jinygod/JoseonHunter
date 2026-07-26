param(
  [Parameter(Mandatory)][string]$Path,
  [int]$ExpectedBottomY = 56,
  [double]$ExpectedCenterX = 32,
  [double]$CenterTolerance = 1,
  [int]$MaxOpaqueColors = 48
)

Add-Type -AssemblyName System.Drawing
$bitmap = [Drawing.Bitmap]::new($Path)
try {
  if ($bitmap.Width -ne 64 -or $bitmap.Height -ne 64) { throw "Expected 64 x 64 PNG; got $($bitmap.Width) x $($bitmap.Height)." }
  if ($bitmap.PixelFormat -notin @([Drawing.Imaging.PixelFormat]::Format32bppArgb, [Drawing.Imaging.PixelFormat]::Format32bppPArgb)) { throw "Expected RGBA pixel format; got $($bitmap.PixelFormat)." }

  $colors = [Collections.Generic.HashSet[int]]::new()
  $minX = 64; $minY = 64; $maxX = -1; $maxY = -1
  for ($y = 0; $y -lt 64; $y++) {
    for ($x = 0; $x -lt 64; $x++) {
      $pixel = $bitmap.GetPixel($x, $y)
      if ($pixel.A -ne 0 -and $pixel.A -ne 255) { throw "Found non-hard alpha at ($x,$y)." }
      if ($pixel.A -gt 0) {
        [void]$colors.Add($pixel.ToArgb())
        $minX = [Math]::Min($minX, $x); $maxX = [Math]::Max($maxX, $x)
        $minY = [Math]::Min($minY, $y); $maxY = [Math]::Max($maxY, $y)
      }
    }
  }
  if ($maxX -lt 0) { throw 'Sprite has no opaque pixels.' }
  foreach ($corner in @(@(0,0), @(63,0), @(0,63), @(63,63))) {
    if ($bitmap.GetPixel($corner[0], $corner[1]).A -ne 0) { throw "Expected transparent corner at ($($corner[0]),$($corner[1]))." }
  }
  if ($colors.Count -gt $MaxOpaqueColors) { throw "Expected at most $MaxOpaqueColors opaque colors; got $($colors.Count)." }
  if ($maxY -ne $ExpectedBottomY) { throw "Expected opaque maxY=$ExpectedBottomY; got $maxY." }
  $centerX = ($minX + $maxX) / 2.0
  if ([Math]::Abs($centerX - $ExpectedCenterX) -gt $CenterTolerance) { throw "Expected horizontal center within $CenterTolerance of $ExpectedCenterX; got $centerX." }

  [pscustomobject]@{ Result = 'PASS'; Dimensions = '64x64'; PixelFormat = $bitmap.PixelFormat.ToString(); OpaqueColors = $colors.Count; Bounds = @($minX, $minY, $maxX, $maxY); CenterX = $centerX; HardAlpha = $true; TransparentCorners = $true } | ConvertTo-Json -Compress
} finally { $bitmap.Dispose() }
