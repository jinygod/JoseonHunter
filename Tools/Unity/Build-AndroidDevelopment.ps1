param()

$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$logs = Join-Path $root 'Logs'
$log = Join-Path $logs 'android-development-build.log'
$apk = Join-Path $root 'Builds\Android\JoseonHunter-development.apk'
$gradleUserHome = 'C:\jh-gradle'

New-Item -ItemType Directory -Path $logs -Force | Out-Null
$asciiPath = [System.Text.Encoding]::ASCII.GetString(
    [System.Text.Encoding]::ASCII.GetBytes($gradleUserHome))
if ($asciiPath -ne $gradleUserHome) {
    throw "GRADLE_USER_HOME must be ASCII-only: $gradleUserHome"
}
New-Item -ItemType Directory -Path $gradleUserHome -Force | Out-Null
$arguments = @(
    '-batchmode', '-nographics', '-quit', '-projectPath', $root,
    '-executeMethod', 'JoseonHunter.Editor.Build.AndroidDevelopmentBuild.Build',
    '-logFile', $log
)
$previousGradleUserHome = $env:GRADLE_USER_HOME
try {
    $env:GRADLE_USER_HOME = $gradleUserHome
    $process = Start-Process -FilePath $unity -ArgumentList $arguments `
        -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) {
        throw "Unity Android development build failed with exit code $($process.ExitCode). Inspect $log."
    }
    if (-not (Test-Path -LiteralPath $apk) -or (Get-Item -LiteralPath $apk).Length -le 0) {
        throw "Unity did not produce a non-empty APK at $apk. Inspect $log."
    }
}
finally {
    $env:GRADLE_USER_HOME = $previousGradleUserHome
}
