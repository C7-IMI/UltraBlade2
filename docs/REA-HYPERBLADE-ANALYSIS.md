# REA analysis of the HyperBlade demo (2026-10-27)

This note records the reverse-engineering pass requested after the first HyperBlade fidelity slice. The working
target was the interactive HyperBlade demo extracted from the retail CD, not the full `HYPERX.EXE` build.

## Tooling and what REA could observe

`C7-IMI/rea` was cloned read-only to `.work/refs/rea` and run with `npx -y rea-agents@latest`. Its provider audit
reported the macOS-native providers as unsupported on this Windows host, and no Ghidra/IDA provider was installed. A
Chocolatey Ghidra install was attempted and failed (no administrator/chocolatey lock); no engine binary was
decompiled by REA. REA's host-independent artifact operations still produced usable evidence:

- `inspect-desktop-application` inventoried `ACTRES/LOGO.DIR`, `HYPER/FILEIO.DLL`, `HYPER/HYPER.DIR`,
  `HYPER/HYPER.EXE`, `HYPER/HYPER.INI` and `HYPER/LINGO.INI` with byte counts and SHA-256 identities.
- `inspect-byte-entropy` measured `HYPER.EXE` at 712,817 bytes (SHA-256
  `0bc0bd01c6ea453e39918c37f5c8af4b5f136c0ddc6350fd1a16fbf88c51c7d2`, entropy 7.6698) and `HYPER.DIR` at
  2,003,612 bytes (SHA-256 `0485535c0a96115f3d07fba94d5608726e83e495ea1891ca4c19d28c7eadecc8`, entropy 7.1274).
  The DEMO EXE is not the 860,160-byte `HYPERX.EXE` game executable; it is the Director-based demo player.
- `inspect-artifact`/`inspect-binary-layout` returned `target_unavailable`/`capability_unavailable` because no native
  provider was present. This is an honest tooling limit, not a claim about the demo.

Raw REA evidence is kept in the git-ignored `.work/refs/demo-analysis/` (`rea-*.json`, UTF-16LE JSON).

## Demo format analysis

The demo `HYPER.DIR` is a Macromedia Director movie (byte-swapped RIFX container, `MV93` type). A parser run
against it found 189 chunks: 60 `Lscr`, 56 `CASt`, 24 `BITD`, 13 `THUM`, 8 `CLUT`, 3 `Lnam`, 2 `snd `, plus the
usual `imap`/`mmap`/`KEY*` tables. No plaintext script strings were present in the top-level chunks; the content is
compiled Lingo and 8-bit bitmap/sound assets. That matches an interactive 1996 demo movie rather than a second
game build.

Because the demo cannot supply the missing gameplay constants on its own, the retail decompilation was used as the
source-level ground truth. `zach2good/hyperblade-decomp` is a byte-matching decompilation of `HYPERX.EXE` v1.0.0.1
(2,805 of 3,054 game functions exact, 480,276 bytes) and `zach2good/hyperblade-20XX` is its port. Those are public
projects, cloned to `.work/refs/` and read-only; their findings are below.

## Findings that changed the game

| Finding | Source | Change in UltraBlade 2 |
| --- | --- | --- |
| Arena collision ellipsoid semi-axes are X=70, Y=125, Z=70 game units; the arena is one large elliptical bowl, not a flat rink | `hyperblade-decomp/src/engine/arenaellipsoidstatic.cpp` | Arena radii 33 x 55, goal lines +/-53, a 6 m curved bowl surface, and a 40 m inward shell |
| Camera projection half-FOV is 85/2 degrees and the rest bungee length is 12; camera tracks a player and uses bungee smoothing | `cameraobject.cpp`, `cameraobject.h` | FOV 85, player-tracking chase camera with distance/height and a drome-boundary clamp instead of the old 58-degree close view |
| The config exposes bird's-eye, first-person and over-shoulder camera offsets; the reference camera is a low third-person chase | `DATA/DFTKEYBD.CFG`, `config.cpp`, visual research | Lower chase rig, no camera spin with player, camera-relative WASD |
| Skaters are humanoid armoured skaters on blades with a left-hand jak; reference frames show skating stride, jump and slash poses | `RESEARCH-VISUAL.md` section 3, demo/video frames | Player rig 1.35x, larger silhouettes, existing stride/slash animation retained and made visible |
| Movement is steering/acceleration on a continuous surface, not a flat-grid CharacterController | `behobjglide*`, `cadjustacceleration*`, `config.cpp` | Transform steering with arena clamp and bowl-surface height; WASD/arrows are camera-relative; no wall collider to stick on |
| Forwards are 2 + AI goalie with a five-man roster; scoring is 1/2/4 through the multiplier; 3 periods | manual/help + prior research | Retained and re-checked in the rebuilt match |

## What was verified after the pass

- `scripts/ublade2_unity.ps1 -Mode build` succeeds with the new camera, bowl and models.
- `scripts/ublade2_unity.ps1 -Mode match-capture` produces a checked 1280x720 frame showing the elliptical bowl,
  the small end goals, the drome wall and a skater; `MATCH_CAPTURE_CHECK_OK`.
- A scripted play-test (`--play-test`) sent W/A/S/D window key messages to the Windows player. It logged W moving
  the player from z -20 to z +18.81 and on to z +47.45, and A moving x from -9 to -18.68; the player followed the
  bowl height and never stuck. This is a real input-path check, not a substitute for a human feel pass.

## Limits

REA native providers were not available, so no decompilation of the demo EXE was performed by REA itself. The demo
contains no playable drome data that could be extracted without a Director authored-asset decoder; the retail
decompilation and video evidence remain the authoritative references. The bowl is a procedural low-poly
approximation: it is not the original drome mesh, the goals are still a derived geometry, and the skater models are
reconstructions rather than the original art.
