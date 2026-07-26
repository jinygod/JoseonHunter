param(
  [Parameter(Mandatory)][string]$InputPath,
  [Parameter(Mandatory)][string]$OutputPath,
  [double]$Scale = 0.9375,
  [int]$OffsetX = 2,
  [int]$OffsetY = 0
)

Add-Type -AssemblyName System.Drawing
$input = [Drawing.Bitmap]::new($InputPath)
try {
  if ($input.Width -ne 64 -or $input.Height -ne 64) { throw 'Input must be exactly 64 x 64.' }
  if ($Scale -le 0 -or $Scale -gt 1) { throw 'Scale must be in the range (0, 1].' }
  $scaledSize = [Math]::Round(64 * $Scale, [MidpointRounding]::AwayFromZero)
  $output = [Drawing.Bitmap]::new(64, 64, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
  try {
    $graphics = [Drawing.Graphics]::FromImage($output)
    try {
      $graphics.Clear([Drawing.Color]::Transparent)
      $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
      $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
      $graphics.DrawImage($input, [Drawing.Rectangle]::new($OffsetX, $OffsetY, $scaledSize, $scaledSize), 0, 0, 64, 64, [Drawing.GraphicsUnit]::Pixel)
    } finally { $graphics.Dispose() }
    $output.Save($OutputPath, [Drawing.Imaging.ImageFormat]::Png)
  } finally { $output.Dispose() }
} finally { $input.Dispose() }
