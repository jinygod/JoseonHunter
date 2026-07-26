param(
  [Parameter(Mandatory)][string[]]$IdleFrames,
  [Parameter(Mandatory)][string[]]$MoveFrames,
  [Parameter(Mandatory)][string[]]$DeathFrames,
  [Parameter(Mandatory)][string]$OutputPath
)

$allFrames = @($IdleFrames) + @($MoveFrames) + @($DeathFrames)
if (@($IdleFrames).Count -ne 2 -or @($MoveFrames).Count -ne 4 -or @($DeathFrames).Count -ne 6) {
  throw 'Expected exactly 2 idle, 4 move, and 6 death frames.'
}

Add-Type -AssemblyName System.Drawing
$images = [Collections.Generic.List[Drawing.Bitmap]]::new()
try {
  foreach ($path in $allFrames) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing frame: $path" }
    $image = [Drawing.Bitmap]::new($path)
    if ($image.Width -ne 64 -or $image.Height -ne 64) { throw "Frame must be 64 x 64: $path" }
    if ($image.PixelFormat -notin @([Drawing.Imaging.PixelFormat]::Format32bppArgb, [Drawing.Imaging.PixelFormat]::Format32bppPArgb)) {
      throw "Frame must be RGBA: $path"
    }
    $images.Add($image)
  }

  $outputDirectory = Split-Path -Parent $OutputPath
  if ($outputDirectory) { New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null }
  $sheet = [Drawing.Bitmap]::new(256, 192, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
  try {
    $graphics = [Drawing.Graphics]::FromImage($sheet)
    try {
      $graphics.Clear([Drawing.Color]::Transparent)
      for ($frame = 0; $frame -lt $images.Count; $frame++) {
        $graphics.DrawImageUnscaled($images[$frame], ($frame % 4) * 64, (2 - [math]::Floor($frame / 4)) * 64)
      }
    } finally { $graphics.Dispose() }
    $sheet.Save($OutputPath, [Drawing.Imaging.ImageFormat]::Png)
  } finally { $sheet.Dispose() }
} finally {
  foreach ($image in $images) { $image.Dispose() }
}
