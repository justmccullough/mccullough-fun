param([string]$EditorPath)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'unity\PasturePong'
$version = ((Get-Content (Join-Path $project 'ProjectSettings\ProjectVersion.txt') -First 1) -split ': ')[1]
if (-not $EditorPath) {
    $EditorPath = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $EditorPath)) {
    throw "Install Unity $version with Web Build Support, or pass -EditorPath."
}
$log = Join-Path $project 'Logs\web-build.log'
New-Item -ItemType Directory -Force -Path (Split-Path $log -Parent) | Out-Null
$arguments = @('-batchmode', '-quit', '-projectPath', "`"$project`"", '-buildTarget', 'WebGL',
    '-executeMethod', 'BuildPasturePong.Build', '-logFile', "`"$log`"")
$process = Start-Process -FilePath $EditorPath -ArgumentList $arguments -PassThru -Wait
if ($process.ExitCode -ne 0) {
    Get-Content -LiteralPath $log -Tail 60
    throw "Unity build failed ($($process.ExitCode)). See $log."
}
if (-not (Select-String -LiteralPath $log -SimpleMatch 'Pasture Pong built successfully.' -Quiet)) {
    throw "Unity exited without confirming the build. See $log."
}
Write-Output 'Unity Web build and game checks passed. Run npm run build to package the site.'
