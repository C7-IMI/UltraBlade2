# UI standard — UltraBlade 2

**Status:** recorded game-side UI/UX standard. It **replaces the parked interface addendum**
([ProjectPack-ADDENDUM-Interface.md](../ProjectPack-ADDENDUM-Interface.md)) for this game and records the decision
**UB2-D3** in [DEFERRED.md](../DEFERRED.md). The addendum is parked, not in force: this document does not quote it as
a requirement and does not partially restore it. This is a game UI standard, not a web design system.

The standard governs the menu, HUD, pause/settings and result screens when they are built. The Unity port is still the
three-collectible gameplay and hero vertical slice, so no screen below is claimed as implemented (**UB2-D14**).
Acceptance evidence for a screen is a run and a read-back capture, not this document alone.

## 1. Rules that apply to every screen

- **One screen, one job.** A screen has a single primary action; secondary actions are visibly secondary.
- **No dead controls.** Every visible control works, or is visibly disabled with the reason. No 'coming soon'
  buttons, fake unlocks, placeholder tabs or controls that only log.
- **Honest labels.** A label names what the control does ('Resume', 'Quit to menu', 'Reset to defaults'). Never claim
  a feature, count, reward or online service that is not there. Show the build version on the menu.
- **State is obvious.** The player can always tell whether they are in the menu, in a match, paused, or looking at a
  result, and how to get back.
- **Input does not hide essentials.** Every action available on touch is reachable with keyboard and mouse, and every
  action available on keyboard is reachable on touch unless the game says otherwise.
- **No essential information by colour alone.** Pair colour with text, icon, shape or position.

## 2. HUD hierarchy and readability

The HUD uses at most three persistent tiers, in this order:

1. **Match-critical** — score, clock/time remaining, objective or collectible progress, and the player's own status.
   Keep these in fixed slots at the top of the safe area. They are always visible during a match.
2. **Tactical** — target indicator, team/opponent status, cooldown or context-sensitive prompt. Show only while
   relevant, near the edge of the action, never covering the play focus.
3. **Transient** — hit feedback, pickups and notifications. These may fade, but must not move the critical tier.

Readability rules:

- Reserve each value's space. Score and clock use tabular numerals and do not reflow when digits change.
- At a 1920x1080 reference, HUD body text is at least 20 px and the score/clock is at least 32 px. Scale from the
  short edge so a 20:9 phone keeps the same physical text size as 16:9.
- Keep the central play area clear except for a reticle, hit marker or a short prompt. Do not put persistent HUD in
  the centre.
- Text over the scene needs a backing scrim, outline or shadow that survives a bright and a dark frame. Thin
  one-pixel lines are not HUD information.
- Critical HUD must remain legible when the background is moving; test on a bright arena and a dark arena.

## 3. Safe areas for 16:9 and 20:9

- Build against both **16:9 (1920x1080)** and **20:9 (2400x1080)**. The 20:9 layout adds horizontal room; it does
  not stretch the HUD or push critical elements under a cutout.
- Anchor to Unity's `Screen.safeArea`, not to the raw screen rectangle. Keep at least 24 px (1080p reference) of
  margin inside the safe area, and never place text or a tap target under a notch, punch-hole or rounded corner.
- Use a short-edge scale reference so vertical sizes stay constant across aspect ratios. Re-check a 4:3 window only
  for editor convenience; 16:9 and 20:9 are the shipping shapes.
- Full-bleed backgrounds may extend outside the safe area; interactive controls, critical HUD and readable text may
  not.
- Verify in the Unity Device Simulator at 16:9 and 20:9, with a cutout profile, before calling a screen accepted.

## 4. Touch targets

- Every tappable control is at least **44 x 44 px** (44 dp at the game's UI scale; 48 x 48 preferred). The visual may
  be smaller only when the invisible hit area still meets the minimum.
- Keep at least 8 dp between adjacent hit areas. Do not put a destructive action next to Resume/Back.
- Pause is a 44 px target in a safe corner and is reachable with the thumb in landscape. Do not require a swipe or a
  long press for a core action.
- Hover is an enhancement, never a requirement. Every mouse control also works by click or keyboard.

## 5. Contrast and legibility

- Body text and essential HUD values: at least **4.5:1** contrast against their effective background.
- Large text (24 px bold or 32 px regular and above) and UI boundaries/icons: at least **3:1**.
- Disabled text must still be identifiable as text; use opacity plus a reason, not invisibility.
- A dynamic background does not excuse low contrast: add a scrim, outline or panel behind the text.
- Check the screen in greyscale; if two states become identical, add a second cue.
- Do not use red/green as the only distinction for score, team or success/failure.

## 6. Menu state machine

The state set is menu, match, pause and result. The normal path is written as:

    menu -> match -> pause -> result

Pause is entered from a live match and returns to it; when the match ends it goes to result. The result screen is
not a pause state.

- **menu** — Start match, Settings, Quit. Settings returns to menu.
- **match** — gameplay; Pause is always available.
- **pause** — Resume, Settings, Restart match, Quit to menu. Settings returns to pause; the match stays paused.
- **result** — Rematch, Return to menu. The result screen cannot be skipped into a new match without an explicit
  Rematch/Start action.

Rules:

- There is one active state at a time. Settings is an overlay on the caller, not a fifth peer state; closing it
  restores the caller exactly.
- `Esc`/Back means 'pause' in match, 'resume' in pause, and 'return' in a menu overlay. It never silently quits a
  live match.
- Entering pause freezes match time and input for the match; it does not reset score, position or cooldowns.
- Result is reached only when the match ends, or by an explicit forfeit/quit action that is labelled as such.
- Restart and quit are confirmed; Resume and Back are not.

## 7. Pause is always available

- From any live match — countdown, play, replay or a non-interactive moment — `Esc`, controller Start/Menu, or the
  on-screen Pause control opens the pause overlay.
- Pause must not be hidden by a cinematic, a tutorial prompt, a network wait or a modal; if another overlay is open,
  pause closes it first or stacks visibly above it.
- Every live match is pausable; the result screen is not. A paused match can always resume or leave to menu.
- Pause does not penalise the player, consume a resource or end the match.

## 8. Settings and persistence

- Settings are versioned and stored under the `ublade2.settings.v1.*` PlayerPrefs namespace. The canonical keys,
  defaults, types and reset rules are in [ROLLBACK.md](./ROLLBACK.md).
- Settings load before the first interactive frame. A missing, unreadable or out-of-range value falls back to its
  default and is clamped; it does not crash, silently reset the whole namespace or block play.
- A change applies immediately where it is safe (audio, sensitivity, display mode). A change that cannot apply
  immediately says so and either applies on the next match or is disabled.
- Settings persist across sessions and builds. Updates and rollbacks must not reset them; only the explicit
  'Reset to defaults' action does, and it confirms first.
- Do not store credentials, purchases, progression authority or anything security-sensitive in PlayerPrefs. The
  league save is a separate versioned file, covered by the rollback plan.

## 9. Keyboard, mouse and touch

- **Keyboard:** WASD/arrows move, Enter/Space confirms, Esc pauses/backs out. All menu items are reachable with
  Tab/Shift+Tab and activate with Enter/Space. Focus is always visible.
- **Mouse:** left click activates; the same hit areas work as touch. No hover-only menu, no right-click-only action.
- **Touch:** landscape-first; 44 px targets; no precision drag or multi-finger gesture required for a core action.
  Swipe may be a shortcut, not the only path.
- **Controller (when supported):** Start/Menu pauses; the face button confirms; B/Back backs out. Do not show
  controller prompts when no controller is connected.
- Show the prompts for the device last used, not a permanent keyboard-plus-touch legend. Never require two input
  devices at once.

## 10. Acceptance checks

A screen is accepted only when all of these are shown:

1. 16:9 and 20:9 captures, with the safe area and a cutout profile, are still legible and fully on-screen.
2. The state machine above can be walked end to end: menu, match, pause, settings from both callers, result,
   rematch, quit to menu.
3. Pause opens from every live-match moment tested; resume restores the same match state.
4. Every control either does its labelled action or is visibly disabled with a reason; no dead control remains.
5. Touch targets measure at least 44 x 44 px; text and essential icons meet the contrast thresholds.
6. Settings survive a restart and a build upgrade; Reset to defaults restores the documented defaults.
7. The build version shown on the menu matches the artifact actually run.

Manual review is the gate for this document. `python scripts/check_all.py` does not test a screen; it only keeps the
repository checks honest. Record captures and the build identity with the result.
