<#
.SYNOPSIS
    Package the PC-only UltraBlade 2 playable build.

.DESCRIPTION
    Reads the version from BuildNotes.md unless -Version is supplied, optionally rebuilds the Windows
    player, then writes dist/UBlade2-<version>-win64.zip and artifacts/ublade2-release/release.json.
    Android is deliberately not packaged: the operator parked mobile on 2026-10-26.

.PARAMETER Version
    Release version used in the ZIP filename and stamp. Defaults to BuildNotes.md "Current version:".

.PARAMETER NoBuild
    Package the existing Windows build instead of running the Unity build wrapper.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ublade2_release.ps1

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ublade2_release.ps1 -NoBuild -Version 2026.10.26.008
#>
param(
  [string]$Version = "",
  [switch]$NoBuild
)

$ErrorActionPreference = "Continue"
$repo = Split-Path -Parent $PSScriptRoot
$winDir = Join-Path $repo "unity/UBlade2/Build"
$exe = Join-Path $winDir "UBlade2.exe"
$distDir = Join-Path $repo "dist"
$outDir = Join-Path $repo "artifacts/ublade2-release"

function Stop-Release {
  param([string]$Message)
  [Console]::Error.WriteLine("UBLADE2_RELEASE_FAIL: $Message")
  exit 1
}

function Write-Utf8NoBom {
  param([string]$Path, [string]$Text)
  $encoding = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllText($Path, $Text, $encoding)
}

function Get-Sha256Hex {
  param([string]$Path)
  return (Get-FileHash -LiteralPath $Path -Algorithm SHA256 -ErrorAction Stop).Hash.ToLowerInvariant()
}

function Get-RepoRelativePath {
  param([string]$Path)
  if ($Path.StartsWith($repo)) {
    return $Path.Substring($repo.Length + 1).Replace('\', '/')
  }
  return $Path.Replace('\', '/')
}

function New-PlayerZip {
  param([string]$ZipPath, [string]$BuildRoot, [string[]]$Items)
  Add-Type -AssemblyName System.IO.Compression -ErrorAction Stop
  Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop
  if (Test-Path -LiteralPath $ZipPath) {
    Remove-Item -LiteralPath $ZipPath -Force -ErrorAction Stop
  }
  $stream = [System.IO.File]::Open($ZipPath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write)
  try {
    $archive = New-Object System.IO.Compression.ZipArchive($stream, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
      foreach ($item in $Items) {
        if (Test-Path -LiteralPath $item -PathType Container) {
          foreach ($file in @(Get-ChildItem -LiteralPath $item -Recurse -File -Force -ErrorAction Stop)) {
            $entryName = $file.FullName.Substring($BuildRoot.Length + 1).Replace('\', '/')
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entryName, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
          }
        } elseif (Test-Path -LiteralPath $item -PathType Leaf) {
          $entryName = $item.Substring($BuildRoot.Length + 1).Replace('\', '/')
          [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $item, $entryName, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        } else {
          throw "missing Windows player input: $item"
        }
      }
    } finally {
      $archive.Dispose()
    }
  } finally {
    $stream.Dispose()
  }
}

try {
  Set-Location -LiteralPath $repo -ErrorAction Stop
  Write-Output "UBLADE2_RELEASE_START platform=windows"

  if ([string]::IsNullOrWhiteSpace($Version)) {
    $buildNotes = Join-Path $repo "BuildNotes.md"
    if (-not (Test-Path -LiteralPath $buildNotes -PathType Leaf)) {
      Stop-Release "BuildNotes.md not found"
    }
    $match = [regex]::Match((Get-Content -LiteralPath $buildNotes -Raw -ErrorAction Stop), 'Current version:\s*([0-9.]+)')
    if (-not $match.Success) {
      Stop-Release "BuildNotes.md has no 'Current version: <number>' line"
    }
    $Version = $match.Groups[1].Value
  }
  if ($Version -notmatch '^[0-9A-Za-z][0-9A-Za-z._-]*$') {
    Stop-Release "invalid version '$Version'"
  }

  if (-not $NoBuild) {
    $buildLog = Join-Path $outDir "build-windows.log"
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    Write-Output "UBLADE2_RELEASE_BUILD windows"
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo "scripts/ublade2_unity.ps1") -Mode build *> $buildLog
    if ($LASTEXITCODE -ne 0) {
      Stop-Release "Windows build failed; see $buildLog"
    }
  }

  if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) {
    Stop-Release "Windows player missing: $exe"
  }

  New-Item -ItemType Directory -Force -Path $distDir | Out-Null
  New-Item -ItemType Directory -Force -Path $outDir | Out-Null
  $zipPath = Join-Path $distDir ("UBlade2-" + $Version + "-win64.zip")

  $dataDir = Join-Path $winDir "UBlade2_Data"
  $unityPlayer = Join-Path $winDir "UnityPlayer.dll"
  $monoDir = Join-Path $winDir "MonoBleedingEdge"
  foreach ($required in @($dataDir, $unityPlayer)) {
    if (-not (Test-Path -LiteralPath $required)) {
      Stop-Release "missing Windows player payload: $required"
    }
  }
  $rootDlls = @(Get-ChildItem -LiteralPath $winDir -Filter *.dll -File -Force -ErrorAction Stop | ForEach-Object { $_.FullName })
  $zipItems = @($exe, $dataDir, $unityPlayer) + $rootDlls
  if (Test-Path -LiteralPath $monoDir) {
    $zipItems += $monoDir
  }
  New-PlayerZip -ZipPath $zipPath -BuildRoot $winDir -Items $zipItems
  $zipInfo = Get-Item -LiteralPath $zipPath -ErrorAction Stop
  if ($zipInfo.Length -lt 20MB) {
    Stop-Release "Windows ZIP is only $($zipInfo.Length) bytes; expected at least 20 MB"
  }
  $zipSha = Get-Sha256Hex -Path $zipPath
  $exeSha = Get-Sha256Hex -Path $exe

  $gitHead = ""
  try {
    $gitHead = (& git -C $repo rev-parse HEAD 2>$null).Trim()
  } catch {
    $gitHead = "unknown"
  }
  $unityVersion = ""
  $versionFile = Join-Path $repo "unity/UBlade2/ProjectSettings/ProjectVersion.txt"
  if (Test-Path -LiteralPath $versionFile) {
    $versionLine = (Get-Content -LiteralPath $versionFile -TotalCount 1 -ErrorAction SilentlyContinue)
    if ($versionLine -match 'm_EditorVersion:\s*(.+)$') {
      $unityVersion = $Matches[1].Trim()
    }
  }
  $now = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
  $zipRel = Get-RepoRelativePath -Path $zipPath
  $exeRel = Get-RepoRelativePath -Path $exe

  $stamp = [ordered]@{
    schema = "ublade2.release/v1"
    platform = "windows"
    version = $Version
    generatedAt = $now
    unityVersion = $unityVersion
    gitHead = $gitHead
    windowsExe = $exeRel
    windowsExeSha256 = $exeSha
    windowsZip = [ordered]@{ path = $zipRel; bytes = [long]$zipInfo.Length; sha256 = $zipSha }
    passed = $true
    note = "PC-only playable slice; unsigned development build. Android/mobile is parked by the operator decision of 2026-10-26 and is not part of this release."
  }
  Write-Utf8NoBom -Path (Join-Path $outDir "release.json") -Text (($stamp | ConvertTo-Json -Depth 5) + [Environment]::NewLine)

  $lines = @(
    "ULTRABLADE 2 - playable slice release",
    ("Version: " + $Version),
    ("Generated: " + $now),
    ("Platform: Windows PC only (mobile parked by operator)"),
    ("Unity: " + $unityVersion),
    ("Git: " + $gitHead),
    "",
    ("Windows player: " + $exeRel),
    ("  exe sha256: " + $exeSha),
    ("Windows ZIP: " + $zipRel),
    ("  bytes:  " + $zipInfo.Length),
    ("  sha256: " + $zipSha),
    "",
    "This is an unsigned development build of the playable slice. Mobile/Android is not part of this release."
  )
  Write-Utf8NoBom -Path (Join-Path $outDir "release.txt") -Text (($lines -join [Environment]::NewLine) + [Environment]::NewLine)

  Write-Output ("UBLADE2_RELEASE_OK version=" + $Version + " win_sha256=" + $zipSha + " package=" + $zipRel)
  exit 0
} catch {
  Stop-Release $_.Exception.Message
}
