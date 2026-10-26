# DEFERRED.md — the work deliberately not done

The register §2.28 requires: what it is, where it is, why it is deferred, what would unblock it, **what would break
if somebody fixed it casually**, and who owns the decision. Read the entry covering an area before you change that
area. An item is closed only with its evidence.

This repository is a game project with working rules, records and one check list, and **no game code yet**. Most
items below are machinery waiting for the thing it guards to exist — they are not silent omissions, and none is
claimed as built.

| ID | What is deferred | Where | Why deferred | Unblocks when | What breaks if fixed casually | Owner |
| --- | --- | --- | --- | --- | --- | --- |
| **UB2-D1** | The engine, version and target selection, and the `project.gypsy.json` manifest | repository root | no design or engine decision has been made for UltraBlade 2 yet; the first game used Godot 4.7.2 but that is not a decision for this one | the operator chooses the engine and targets; then run `gypsy project init` | a guessed manifest pins an engine nobody chose and makes every later check a lie | operator |
| **UB2-D2** | The game tree, build, headless demo and capture verification | `game/`, `build/`, `artifacts/` | nothing exists to build or run yet | the engine is chosen and the first slice is planned | a build claim without a build is false evidence (Part 11.6) | the first build slice |
| **UB2-D3** | The game's interface design and UI/UX standard | `ProjectPack-ADDENDUM-Interface.md` | the web pack's interface rules were parked because this is a game; no game-side standard exists yet (see `ProjectPack-NOTES.md` §8.1) | the operator decides whether to restore the addendum, replace it with a game-side standard, or defer further | a half-restored rule contradicts Part 11.1 and the parked inventory | operator |
| **UB2-D4** | Generated documentation copies and the game's generated artefacts | scripts, `build/`, `artifacts/` | there is no generated documentation artefact; the game export and captures arrive with the game | the first generated artefact exists; then add its generator with a `--check` mode | hand-editing a generated file is the drift §2.5 exists to prevent | the change that adds the generator |
| **UB2-D5** | A generated contract artefact and its guide | docs | no API or published interface exists; if the game never exposes one this stays closed as "not applicable" | the game exposes an API or message contract | a hand-written contract drifts from the API the moment the API exists | operator decision when an API appears |
| **UB2-D6** | Rollback documents for configurable layers, and the first application of the expand-only rule | docs | nothing is configurable at the layer level and nothing is retrofitted yet | the first configurable layer (graphics/difficulty/feature flag) or live data exists | a rollback document for a layer that does not exist is fiction | the change that adds it |
| **UB2-D7** | A mechanised peer-project boundary guard | scripts | Part 11.5's guard needs a peer list and records to compare; both are new, the guard is not built | the first guard implementation | a guard written before the records settle compares nothing and reports green | first build slice |
| **UB2-D8** | Secret scan over the working tree and history | scripts | no code or configuration carries a credential yet | the first code or configuration key exists | a scanner added later cannot undo a credential already committed; add it before the first secret-bearing file | the change that adds the first credential-bearing configuration |
| **UB2-D9** | Build/test check rows in `scripts/check_all.py` (typecheck, export, headless demo, tests) | `scripts/check_all.py` | no game code, engine or test suite exists | the game tree and its build/test commands exist | adding a check that cannot fail is worse than no check (§1.5) | the first build slice |
| **UB2-D10** | Machine-level automation: scheduled tasks, boot scripts, runner shims | scripts or startup | nothing runs on the operator's machine as part of this project yet | something needs to run on a schedule or at boot | automation registered without its script is a machine-level orphan | the change that needs it |
| **UB2-D11** | Stamped distributable artefacts | build | nothing is distributed | the first installer or client build | shipping a changed payload under an old identity (§2.8) | the release slice |
| **UB2-D12** | Deploy preflight, environment contract and workflow-shell checks | .github, scripts | nothing is deployed and there is no infrastructure | the first deployable service or template exists | a preflight for a deploy that does not exist is decoration | the change that adds a deployment |

## Standing note

The interface design/redesign material is **parked, not deferred work to complete**: it is complete and preserved
verbatim in `ProjectPack-ADDENDUM-Interface.md`. UB2-D3 is the decision that either restores it, replaces it with a
game-side standard, or leaves it parked longer. Do not treat "parked" as "forgotten", and do not partially apply it.

## Closed

Nothing is closed yet.
