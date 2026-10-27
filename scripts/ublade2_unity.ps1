param(
  [ValidateSet("inspect","build","demo","capture","android","match","match-capture","menu-capture","league-test","ui-capture")]
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

if ($Mode -eq "ui-capture") {
  if (-not (Test-Path $exe)) { Write-Error "UBlade2.exe missing; run -Mode build first"; exit 1 }
  $gameDir = Join-Path $repo "artifacts/ublade2-pro"
  $uiDir = Join-Path $gameDir "ui"
  New-Item -ItemType Directory -Force -Path $uiDir | Out-Null
  $tempLog = Join-Path $env:TEMP "ublade2-ui-capture.log"
  $tempDir = Join-Path $env:TEMP "ublade2-ui-capture-frames"
  Remove-Item $tempLog -Force -ErrorAction SilentlyContinue
  Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
  New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
  $arguments = @("--ui-capture=$tempDir","-screen-fullscreen","0","-screen-width","1280","-screen-height","720","-logFile",$tempLog)
  $process = Start-Process -FilePath $exe -WorkingDirectory (Join-Path $repo "unity/UBlade2/Build") -ArgumentList $arguments -PassThru -WindowStyle "Normal"
  for ($i = 0; $i -lt 180; $i++) { Start-Sleep -Milliseconds 500; if ($process.HasExited) { break } }
  if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force; Write-Error "player did not exit"; exit 1 }
  Copy-Item $tempLog (Join-Path $gameDir "ui-capture.log") -Force
  if (-not (Select-String -Path (Join-Path $gameDir "ui-capture.log") -Pattern "UBLADE2_UI_CAPTURE_OK" -Quiet)) { Write-Error "UI capture marker missing"; exit 1 }
  if ($process.ExitCode -ne 0) { Write-Error "UI capture exited $($process.ExitCode)"; exit 1 }
  Get-ChildItem $tempDir -Filter '*.png' | ForEach-Object { Copy-Item $_.FullName (Join-Path $uiDir $_.Name) -Force }
  python scripts/check_ui_captures.py --dir artifacts/ublade2-pro/ui --out artifacts/ublade2-pro/ui-captures.json 2>&1
  if ($LASTEXITCODE -ne 0) { Write-Error "UI captures check failed"; exit 1 }
  python scripts/normalize_evidence.py --paths artifacts/ublade2-pro 2>&1 | Out-Null
  Write-Output "UBLADE2_GAME_UI_CAPTURE_OK dir=artifacts/ublade2-pro/ui json=artifacts/ublade2-pro/ui-captures.json"
  exit 0
}

if ($Mode -eq "league-test") {
  if (-not (Test-Path $exe)) { Write-Error "UBlade2.exe missing; run -Mode build first"; exit 1 }
  $gameDir = Join-Path $repo "artifacts/ublade2-pro"
  New-Item -ItemType Directory -Force -Path $gameDir | Out-Null
  $tempLog = Join-Path $env:TEMP "ublade2-league-test.log"
  $tempEvidence = Join-Path $env:TEMP "ublade2-league-test-runtime.json"
  Remove-Item $tempLog,$tempEvidence -Force -ErrorAction SilentlyContinue
  $arguments = @("-batchmode","-nographics","--league-test","--evidence=$tempEvidence","-logFile",$tempLog)
  $process = Start-Process -FilePath $exe -WorkingDirectory (Join-Path $repo "unity/UBlade2/Build") -ArgumentList $arguments -PassThru -WindowStyle "Hidden"
  for ($i = 0; $i -lt 120; $i++) { Start-Sleep -Milliseconds 500; if ($process.HasExited) { break } }
  if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force; Write-Error "player did not exit"; exit 1 }
  if (-not (Test-Path $tempLog)) { Write-Error "player log missing"; exit 1 }
  Copy-Item $tempLog (Join-Path $gameDir "league-test.log") -Force
  if (-not (Select-String -Path (Join-Path $gameDir "league-test.log") -Pattern "UBLADE2_LEAGUE_SAVE_OK" -Quiet)) { Write-Error "league save marker missing"; exit 1 }
  if (-not (Select-String -Path (Join-Path $gameDir "league-test.log") -Pattern "passed=True" -Quiet)) { Write-Error "league save check failed"; exit 1 }
  if ($process.ExitCode -ne 0) { Write-Error "league test exited $($process.ExitCode)"; exit 1 }
  if (Test-Path $tempEvidence) { Copy-Item $tempEvidence (Join-Path $gameDir "league-save.json") -Force }
  python scripts/normalize_evidence.py --paths artifacts/ublade2-pro 2>&1 | Out-Null
  Write-Output "UBLADE2_GAME_LEAGUE_OK log=artifacts/ublade2-pro/league-test.log evidence=artifacts/ublade2-pro/league-save.json exit=0"
  exit 0
}

if ($Mode -eq "match" -or $Mode -eq "match-capture" -or $Mode -eq "menu-capture") {
  if (-not (Test-Path $exe)) { Write-Error "UBlade2.exe missing; run -Mode build first"; exit 1 }
  $gameDir = Join-Path $repo "artifacts/ublade2-pro"
  New-Item -ItemType Directory -Force -Path $gameDir | Out-Null
  $tempLog = Join-Path $env:TEMP ("ublade2-" + $Mode + ".log")
  $tempFrame = Join-Path $env:TEMP ("ublade2-" + $Mode + "-frame.png")
  $tempEvidence = Join-Path $env:TEMP ("ublade2-" + $Mode + "-runtime.json")
  Remove-Item $tempLog,$tempFrame,$tempEvidence -Force -ErrorAction SilentlyContinue
  if ($Mode -eq "match") {
    $arguments = @("-batchmode","-nographics","--match","--evidence=$tempEvidence","-logFile",$tempLog)
    $windowStyle = "Hidden"
    $marker = "UBLADE2_MATCH_OK"
    $logName = "match.log"
  } elseif ($Mode -eq "match-capture") {
    $arguments = @("--match-capture=$tempFrame","--evidence=$tempEvidence","-screen-fullscreen","0","-screen-width","1280","-screen-height","720","-logFile",$tempLog)
    $windowStyle = "Normal"
    $marker = "UBLADE2_MATCH_CAPTURE_OK"
    $logName = "match-capture.log"
  } else {
    $arguments = @("--menu-capture=$tempFrame","-screen-fullscreen","0","-screen-width","1280","-screen-height","720","-logFile",$tempLog)
    $windowStyle = "Normal"
    $marker = "UBLADE2_MENU_CAPTURE_OK"
    $logName = "menu-capture.log"
  }
  $process = Start-Process -FilePath $exe -WorkingDirectory (Join-Path $repo "unity/UBlade2/Build") -ArgumentList $arguments -PassThru -WindowStyle $windowStyle
  for ($i = 0; $i -lt 360; $i++) {
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
  $playerLog = Join-Path $gameDir $logName
  Copy-Item $tempLog $playerLog -Force
  if (-not (Select-String -Path $playerLog -Pattern $marker -Quiet)) { Write-Error ($marker + " missing from player log"); exit 1 }
  if ($exitCode -ne 0) { Write-Error "player exited $exitCode"; exit 1 }
  if ($Mode -eq "match") { if (Test-Path $tempEvidence) { Copy-Item $tempEvidence (Join-Path $gameDir "match-runtime.json") -Force } }
  else { if (Test-Path $tempEvidence) { Copy-Item $tempEvidence (Join-Path $gameDir "match-capture-runtime.json") -Force } }
  if ($Mode -eq "match") {
    Write-Output "UBLADE2_GAME_MATCH_OK log=artifacts/ublade2-pro/match.log evidence=artifacts/ublade2-pro/match-runtime.json exit=0"
    python scripts/normalize_evidence.py --paths artifacts/ublade2-pro 2>&1 | Out-Null
    exit 0
  }
  if (-not (Test-Path $tempFrame)) { Write-Error "capture frame missing"; exit 1 }
  if ($Mode -eq "match-capture") {
    $frame = Join-Path $gameDir "match.png"
    $captureJson = Join-Path $gameDir "match-capture.json"
    Copy-Item $tempFrame $frame -Force
    python scripts/check_match_capture.py --kind match --frame "artifacts/ublade2-pro/match.png" --evidence "artifacts/ublade2-pro/match-runtime.json" --out "artifacts/ublade2-pro/match-capture.json" 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Error "match capture check failed"; exit 1 }
    Write-Output "UBLADE2_GAME_MATCH_CAPTURE_OK frame=artifacts/ublade2-pro/match.png json=artifacts/ublade2-pro/match-capture.json"
  } else {
    $frame = Join-Path $gameDir "menu.png"
    Copy-Item $tempFrame $frame -Force
    python scripts/check_match_capture.py --kind menu --frame "artifacts/ublade2-pro/menu.png" --out "artifacts/ublade2-pro/menu-capture.json" 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Error "menu capture check failed"; exit 1 }
    Write-Output "UBLADE2_GAME_MENU_CAPTURE_OK frame=artifacts/ublade2-pro/menu.png json=artifacts/ublade2-pro/menu-capture.json"
  }
  python scripts/normalize_evidence.py --paths artifacts/ublade2-pro 2>&1 | Out-Null
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
