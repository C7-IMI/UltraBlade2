# Retrace - the prompt log

Retrace is UltraBlade 2's prompt log (`<PROMPT_LOG>` in the pack). Every request is recorded here in the order it
was made: its date, the request, its status, and the commit or evidence it produced. The newest entry is at the
bottom and the prompt numbers are contiguous from 1.

Read it with [`BuildNotes.md`](./BuildNotes.md), the dated versioned changelog. `python scripts/records_check.py` (run
by `python scripts/check_all.py`) fails if an entry loses its number or status, if BuildNotes is not newest-first, if
BuildNotes' `Current version:` header disagrees with its newest heading, or if a version repeats.

## How this log was started

This log starts on 2026-10-26 with the repository's first two prompts. Both were asked in the same session, and the
pack was applied in that session: entry 1 is the repository creation, entry 2 is the pack application. The request
lines are the prompts as written (they were terse); per-prompt durations were not preserved, and that gap is stated
rather than invented. From entry 3 on, the request is appended as written and the record is written in the same
commit as the change it describes.

Each entry has:

- **Request** - the prompt as written.
- **Status** - Completed, In progress or Blocked. A request that changed nothing says so explicitly.
- **Version** - the BuildNotes version the change produced, or why no version belongs to it.
- **Commit/evidence** - the commit hash, or the file and observation that are the evidence.

## 1 - 2026-10-26

- Request: "Create a new repo called UltraBlade 2. Make it public"
- Status: Completed
- Version: 2026.10.26.001
- Commit/evidence: `2ee21f9` - the initial commit. The repository https://github.com/C7-IMI/UltraBlade2 was verified
  public with `gh repo view C7-IMI/UltraBlade2 --json visibility,url` (`"visibility":"PUBLIC"`) and with an
  unauthenticated `GET /repos/C7-IMI/UltraBlade2` that returned HTTP 200; `main` tracks `origin/main`.

## 2 - 2026-10-26

- Request: "copy the ProjectPack from ProjectGypsy to this repo, read it, then apply it to this session"
- Status: Completed
- Version: 2026.10.26.002
- Commit/evidence: the pack-application commit - `ProjectPack.md` (revision UB2-1),
  `ProjectPack-ADDENDUM-Interface.md`, `ProjectPack-NOTES.md`, `AGENTS.md`, `DEFERRED.md`, this log,
  `BuildNotes.md`, `scripts/next-version.py`, `scripts/records_check.py`, `scripts/check_all.py`,
  `scripts/install-hooks.py`, `.githooks/pre-commit` and `.github/workflows/checks.yml`. The application is verified
  in the table at the end of `ProjectPack-NOTES.md`; `python scripts/check_all.py` prints the check list's result,
  recorded in `BuildNotes.md`.
