# BuildNotes - the changelog

Current version: 2026.10.26.002

BuildNotes is UltraBlade 2's dated, versioned changelog (`<CHANGELOG>` in the pack). Newest entry first; versions
are `YYYY.M.D.NNN` with a zero-padded sequence, and the next one is computed by
[`scripts/next-version.py`](./scripts/next-version.py) rather than typed. [`Retrace.md`](./Retrace.md) is the prompt
log these entries answer. Records are guarded by [`scripts/records_check.py`](./scripts/records_check.py) and run by
[`scripts/check_all.py`](./scripts/check_all.py).

## How versions 001-002 were assigned

No changelog existed before this bootstrap, so the bootstrap assigned `2026.10.26.001` to the repository-creation
commit (the only commit already in `git log`) and `2026.10.26.002` to the pack application. From the next version on,
the number is computed by the script.

This changelog has **no generated documentation copies yet**. There is no generated documentation artefact to
regenerate; when the game arrives, its export and captures will be generated artefacts under gitignored `build/` and
`artifacts/`. [`scripts/records_check.py`](./scripts/records_check.py) checks the source, its `Current version:` header
and [`Retrace.md`](./Retrace.md); it will compare generated copies when they exist.

## 2026.10.26.002

**Apply the ProjectPack from ProjectGypsy, and bootstrap the records and check list.**

- Date: 2026-10-26
- Commit: this change.
- Change: copied ProjectGypsy's pack revision PG-1 into `ProjectPack.md` as revision **UB2-1** (Parts 1-10 carried;
  header and Part 11 re-authored for this repository), carried the parked interface material into
  `ProjectPack-ADDENDUM-Interface.md`, and wrote `ProjectPack-NOTES.md` resolving the placeholders, the §8.1
  decisions, the §2.31 in-force list, the §8.2 check inventory and the Part 11 mapping. Added `AGENTS.md` (pack
  rules plus the Gypsy Studio invocation block), `DEFERRED.md`, the prompt log `Retrace.md`, this changelog,
  `scripts/next-version.py`, `scripts/records_check.py`, `scripts/check_all.py`, `scripts/install-hooks.py`, the
  tracked `.githooks/pre-commit` and `.github/workflows/checks.yml`. `README.md` updated to point at the rules and
  the records.
- Verification: `python scripts/check_all.py` -> `checks: PASS (4/4)` (records, encoding, links, callers);
  `python scripts/install-hooks.py` -> `core.hooksPath = .githooks` (read back from git config);
  `ProjectPack-NOTES.md` "How this application of the pack was verified" records the provenance hashes and the diff
  Every check was also shown able to fail in a throwaway copy: a broken BuildNotes header made `records`
  FAIL; a staged BOM plus a broken link made `encoding` and `links` FAIL; a hook argument made `callers` FAIL
  (`checks: FAIL (0/4 passed)`). The copy was deleted afterwards.
  scope. Not verified, and not claimed: no game build, test suite, engine choice, `project.gypsy.json` manifest or
  design document exists (UB2-D1, UB2-D2).

## 2026.10.26.001

**Initial commit: UltraBlade 2 project workspace.**

- Date: 2026-10-26
- Commit: `2ee21f9`.
- Change: `README.md`, `.gitignore` and `.gitattributes` for the new repository; the public GitHub repository
  C7-IMI/UltraBlade2 created under the C7-IMI account (owner `C7-IMI`).
- Verification: `gh repo view C7-IMI/UltraBlade2 --json visibility,url` -> PUBLIC and the repository URL; an
  unauthenticated `GET /repos/C7-IMI/UltraBlade2` returned HTTP 200.
