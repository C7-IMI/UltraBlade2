param(
  [ValidateSet("device","demo")] [string]$Mode = "device",
  [string]$Serial = "",
  [string]$Evidence = "artifacts/ublade2-mobile"
)
$ErrorActionPreference = "Continue"
$repo = Split-Path $PSScriptRoot -Parent
Set-Location $repo
$sdk = "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK"
$adb = Join-Path $sdk "platform-tools\adb.exe"
$apk = Join-Path $repo "unity/UBlade2/Build/Android/UBlade2.apk"
$out = Join-Path $repo $Evidence
New-Item -ItemType Directory -Force -Path $out | Out-Null
if (-not (Test-Path $adb)) { Write-Error "adb missing at $adb"; exit 1 }
if (-not (Test-Path $apk)) { Write-Error "APK missing; run -Mode android first"; exit 1 }

if (-not $Serial) {
  $devices = (& $adb devices 2>&1) -join [Environment]::NewLine
  foreach ($candidate in @("emulator-40000","127.0.0.1:40001")) {
    if ($devices -match ([regex]::Escape($candidate) + "\s+device")) { $Serial = $candidate; break }
  }
  if (-not $Serial) {
    $match = $devices | Select-String -Pattern "^(emulator-\d+|127\.0\.0\.1:\d+)\s+device$" | Select-Object -First 1
    if ($match) { $Serial = $match.Matches[0].Groups[1].Value }
  }
}
if (-not $Serial) { Write-Error "no adb device; start the emulator first"; exit 1 }

& $adb -s $Serial wait-for-device 2>&1 | Out-Null
& $adb -s $Serial shell settings put secure immersive_mode_confirmations confirmed 2>&1 | Out-Null
Write-Output "UBLADE2_ANDROID_DEVICE serial=$Serial"
& $adb -s $Serial install -r $apk 2>&1 | Select-Object -Last 2
if ($LASTEXITCODE -ne 0) { Write-Error "apk install failed"; exit 1 }

$props = @(
  "serial=$Serial",
  ("abilist=" + ((& $adb -s $Serial shell getprop ro.product.cpu.abilist 2>&1) -join "")),
  ("release=" + ((& $adb -s $Serial shell getprop ro.build.version.release 2>&1) -join "")),
  ("sdk=" + ((& $adb -s $Serial shell getprop ro.build.version.sdk 2>&1) -join "")),
  ("native_bridge=" + ((& $adb -s $Serial shell getprop ro.dalvik.vm.native.bridge 2>&1) -join "")),
  ("wm_size=" + ((& $adb -s $Serial shell wm size 2>&1) -join ""))
)
$props | Out-File -FilePath (Join-Path $out "device-info.txt") -Encoding utf8

if ($Mode -eq "demo") {
  & $adb -s $Serial shell am force-stop com.c7imi.ublade2 2>&1 | Out-Null
  & $adb -s $Serial logcat -c 2>&1 | Out-Null
  & $adb -s $Serial shell am start -n com.c7imi.ublade2/com.unity3d.player.UnityPlayerGameActivity --ez ublade2_demo true 2>&1
  $deadline = (Get-Date).AddSeconds(90)
  $found = $false
  while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 5
    $log = (& $adb -s $Serial logcat -d 2>&1) -join [Environment]::NewLine
    if ($log -match "GYPSY_DEMO_OK collected=3") { $found = $true; break }
  }
  & $adb -s $Serial logcat -d 2>&1 | Out-File -FilePath (Join-Path $out "logcat-demo.txt") -Encoding utf8
  python scripts/normalize_evidence.py --paths artifacts/ublade2-mobile 2>&1 | Out-Null
  if (-not $found) { Write-Error "GYPSY_DEMO_OK marker missing"; exit 1 }
  Write-Output "UBLADE2_ANDROID_DEMO_OK marker=GYPSY_DEMO_OK collected=3 serial=$Serial"
  exit 0
}

& $adb -s $Serial shell am force-stop com.c7imi.ublade2 2>&1 | Out-Null
& $adb -s $Serial logcat -c 2>&1 | Out-Null
& $adb -s $Serial shell am start -n com.c7imi.ublade2/com.unity3d.player.UnityPlayerGameActivity 2>&1
$ready = $false
$deadline = (Get-Date).AddSeconds(120)
while ((Get-Date) -lt $deadline) {
  Start-Sleep -Seconds 5
  $log = (& $adb -s $Serial logcat -d 2>&1) -join [Environment]::NewLine
  if ($log -match "UBLADE_HERO_OK") { $ready = $true; break }
}
& $adb -s $Serial shell screencap -p /sdcard/ublade2-screen.png 2>&1 | Out-Null
& $adb -s $Serial pull /sdcard/ublade2-screen.png (Join-Path $out "screen.png") 2>&1 | Out-Null
& $adb -s $Serial logcat -d 2>&1 | Out-File -FilePath (Join-Path $out "logcat-launch.txt") -Encoding utf8
python scripts/normalize_evidence.py --paths artifacts/ublade2-mobile 2>&1 | Out-Null
if (-not (Test-Path (Join-Path $out "screen.png"))) { Write-Error "screenshot missing"; exit 1 }
$log = Get-Content (Join-Path $out "logcat-launch.txt") -Raw
if ($log -notmatch "UBLADE2_START") { Write-Error "UBLADE2_START marker missing"; exit 1 }
if ($log -notmatch "UBLADE_HERO_OK") { Write-Error "UBLADE_HERO_OK marker missing"; exit 1 }
Write-Output "UBLADE2_ANDROID_DEVICE_OK serial=$Serial screenshot=artifacts/ublade2-mobile/screen.png"
exit 0
