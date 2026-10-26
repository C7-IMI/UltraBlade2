param(
  [ValidateSet("inspect","build","demo","capture","android")]
  [string]$Mode = "build",
  [string]$Evidence = "artifacts/ublade2/hero-runtime.json"
)
$ErrorActionPreference = "Continue"
$repo = Split-Path $PSScriptRoot -Parent
Set-Location $repo

$unity = "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe"
$quest = "unity/UBlade2"
$exe = Join-Path $repo "unity/UBlade2/Build/UBlade2.exe"
$logPath = Join-Path $repo "artifacts/ublade2/build.log"
New-Item -ItemType Directory -Force -Path (Split-Path $logPath) | Out-Null
Remove-Item (Join-Path $repo "$quest/Temp/UnityLockfile") -Force -ErrorAction SilentlyContinue

if ($Mode -eq "inspect" -or $Mode -eq "build") {
  Remove-Item $logPath -Force -ErrorAction SilentlyContinue
  $method = if ($Mode -eq "build") { "BuildUBlade2.Build" } else { "BuildUBlade2.Inspect" }
  & $unity -batchmode -quit -nographics -projectPath (Join-Path $repo $quest) -executeMethod $method -logFile $logPath 2>&1 | Select-Object -Last 6
  $editorExit = $LASTEXITCODE
  if (-not (Test-Path $logPath)) { Write-Error "Unity log missing"; exit 1 }
  if ($Mode -eq "inspect") {
    if (-not (Select-String -Path $logPath -Pattern "WAYFARER_IMPORT_OK" -Quiet)) { Write-Error "inspect marker missing"; exit 1 }
    Write-Output "UBLADE2_INSPECT_OK log=artifacts/ublade2/build.log"
    exit 0
  }
  if (-not (Test-Path $exe)) { Write-Error "UBlade2.exe missing"; exit 1 }
  python scripts/normalize_evidence.py --paths artifacts/ublade2 artifacts/hero 2>&1 | Out-Null
  if (-not (Select-String -Path $logPath -Pattern "UBLADE2_BUILD Succeeded" -Quiet)) { Write-Error "build success marker missing"; exit 1 }
  Write-Output "UBLADE2_BUILD_OK path=unity/UBlade2/Build/UBlade2.exe editorExit=$editorExit"
  exit 0
}

if ($Mode -eq "android") {
  $androidLog = Join-Path $repo "artifacts/ublade2-android/build.log"
  New-Item -ItemType Directory -Force -Path (Split-Path $androidLog) | Out-Null
  Remove-Item $androidLog -Force -ErrorAction SilentlyContinue
  & $unity -batchmode -quit -nographics -projectPath (Join-Path $repo $quest) -buildTarget Android -executeMethod BuildUBlade2.BuildAndroid -logFile $androidLog 2>&1 | Select-Object -Last 6
  $androidExit = $LASTEXITCODE
  $apk = Join-Path $repo "unity/UBlade2/Build/Android/UBlade2.apk"
  if (-not (Test-Path $apk)) { Write-Error "UBlade2.apk missing"; exit 1 }
  if (-not (Select-String -Path $androidLog -Pattern "UBLADE2_ANDROID_BUILD Succeeded" -Quiet)) { Write-Error "android build success marker missing"; exit 1 }
  python scripts/normalize_evidence.py --paths artifacts/ublade2-android 2>&1 | Out-Null
  Write-Output "UBLADE2_ANDROID_OK apk=unity/UBlade2/Build/Android/UBlade2.apk editorExit=$androidExit"
  exit 0
}

if (-not (Test-Path $exe)) { Write-Error "UBlade2.exe missing; run the build first"; exit 1 }
$playerLog = Join-Path $repo ("artifacts/ublade2/" + $Mode + ".log")
$tempLog = Join-Path $env:TEMP ("ublade2-" + $Mode + ".log")
$tempEvidence = Join-Path $env:TEMP ("ublade2-" + $Mode + "-hero-runtime.json")
$tempFrame = Join-Path $env:TEMP ("ublade2-" + $Mode + "-frame.png")
Remove-Item $tempLog,$tempEvidence,$tempFrame -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path (Split-Path $playerLog) | Out-Null

if ($Mode -eq "demo") {
  $arguments = @("-batchmode","-nographics","--demo","--evidence=$tempEvidence","-logFile",$tempLog)
  $windowStyle = "Hidden"
} else {
  $arguments = @("--demo","--capture=$tempFrame","--evidence=$tempEvidence","-screen-fullscreen","0","-screen-width","1280","-screen-height","720","-logFile",$tempLog)
  $windowStyle = "Normal"
}
$process = Start-Process -FilePath $exe -WorkingDirectory (Join-Path $repo "unity/UBlade2/Build") -ArgumentList $arguments -PassThru -WindowStyle $windowStyle
for ($i = 0; $i -lt 240; $i++) {
  Start-Sleep -Milliseconds 500
  if ($process.HasExited) { break }
}
if (-not $process.HasExited) {
  Stop-Process -Id $process.Id -Force
  Write-Error "player did not exit"
  exit 1
}
$exitCode = $process.ExitCode
if (-not (Test-Path $tempLog)) { Write-Error "player log missing"; exit 1 }
Copy-Item $tempLog $playerLog -Force
if (-not (Select-String -Path $playerLog -Pattern "GYPSY_DEMO_OK collected=3" -Quiet)) { Write-Error "demo marker missing"; exit 1 }
if ($exitCode -ne 0) { Write-Error "player exited $exitCode"; exit 1 }
if ($Mode -eq "capture") {
  if (-not (Test-Path $tempFrame)) { Write-Error "capture frame missing"; exit 1 }
  Copy-Item $tempFrame (Join-Path $repo "artifacts/ublade2/frame.png") -Force
  Write-Output "UBLADE2_CAPTURE_OK frame=artifacts/ublade2/frame.png"
} else {
  Write-Output "UBLADE2_DEMO_OK exit=0"
}
if (Test-Path $tempEvidence) { Copy-Item $tempEvidence (Join-Path $repo $Evidence) -Force }
python scripts/normalize_evidence.py --paths artifacts/ublade2 artifacts/hero 2>&1 | Out-Null
exit 0
