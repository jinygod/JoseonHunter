$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$packer = Join-Path $root 'Tools\Assets\Pack-FrontFacingCharacterSheet.ps1'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('front-facing-pack-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture | Out-Null
Add-Type -AssemblyName System.Drawing
try {
  $frames = [Collections.Generic.List[string]]::new()
  for ($frame = 0; $frame -lt 12; $frame++) {
    $path = Join-Path $fixture ("frame-{0:D2}.png" -f $frame)
    $bitmap = [Drawing.Bitmap]::new(64, 64, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
      $color = [Drawing.Color]::FromArgb(255, $frame + 1, 0, 0)
      for ($y = 0; $y -lt 64; $y++) { for ($x = 0; $x -lt 64; $x++) { $bitmap.SetPixel($x, $y, $color) } }
      $bitmap.Save($path, [Drawing.Imaging.ImageFormat]::Png)
    } finally { $bitmap.Dispose() }
    $frames.Add($path)
  }
  $output = Join-Path $fixture 'sheet.png'
  & $packer -IdleFrames $frames.GetRange(0, 2) -MoveFrames $frames.GetRange(2, 4) -DeathFrames $frames.GetRange(6, 6) -OutputPath $output
  $sheet = [Drawing.Bitmap]::new($output)
  try {
    foreach ($mapping in @(@(0, 0, 128), @(2, 128, 128), @(6, 128, 64))) {
      $pixel = $sheet.GetPixel($mapping[1], $mapping[2])
      if ($pixel.R -ne ($mapping[0] + 1)) { throw "Frame $($mapping[0]) did not map to its Unity contract cell." }
    }
  } finally { $sheet.Dispose() }
} finally {
  Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction SilentlyContinue
}
