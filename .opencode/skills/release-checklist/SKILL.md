---
name: release-checklist
description: Runs the release readiness gate before a tagged release or version bump. The gate checks the SPEC-000 and SPEC-002 release criteria for the minor, rc or final profile and renders the sign-off file. Triggers when preparing a tagged release or version bump.
---

# Release Checklist Skill

The checklist is executable: do not keep a second copy of it here. The registry `.github/release-checklist.json` is the single list of items, each traced to a SPEC-000 or SPEC-002 acceptance criterion or to #104. The tool is `.github/scripts/release-readiness.cs` (header: modes, exit codes, facts and sign-off formats).

## Run the gate

Pick the release branch `release/v<version>`; the profile follows from the version (`-rc.N` is `rc`, `1.0.0` is `final`, any other `X.Y.0` is `minor`; a name that maps to none is an error).

```powershell
dotnet run --file .github/scripts/release-readiness.cs -- --check-registry
dotnet run --file .github/scripts/release-readiness.cs -- --render-checklist --profile minor --version 0.14.0 --out docs/releases/v0.14.0/checklist.md
dotnet run --file .github/scripts/release-readiness.cs -- --collect --from-branch release/v0.14.0 --out docs/releases/v0.14.0/facts.json
dotnet run --file .github/scripts/release-readiness.cs -- --evaluate --facts docs/releases/v0.14.0/facts.json --from-branch release/v0.14.0 --signoff docs/releases/v0.14.0/checklist.md
dotnet run --file .github/scripts/release-readiness.cs -- --evidence --facts docs/releases/v0.14.0/facts.json --signoff docs/releases/v0.14.0/checklist.md --out docs/releases/v0.14.0/evidence.md
```

- Exit 0 means every blocking item passed; exit 1 means at least one failed or is unanswered; exit 2 is a usage or registry error.
- Answer each manual item in `checklist.md` by ticking it and adding an evidence link. A ticked line without a link counts as unanswered.
- Coverage and mutation have no project-wide percentage: the gate checks the per-flag targets and the dashboard freshness (AGENTS.md section 9, SPEC-000 REQ-007 and REQ-008).
- Secret-scanning and Dependabot alerts need the GitHub App token in `RELEASE_GATE_APP_TOKEN`; without it those items fail closed.

## Release flow

1. Merge feature branches to main.
2. Fold the changelog fragments: `dotnet run .github/scripts/changelog-fragments.cs -- --release <version> <yyyy-MM-dd> [title]`.
3. Run the gate (above) and commit `checklist.md`, `facts.json` and `evidence.md` in the release PR.
4. Before tagging, re-check the merge commit: `--collect --sha <merge-sha>` into a scratch facts file, then `--evaluate --only ci-full-on-sha` on it.
5. Follow `docs/releases/RELEASE-PROCESS.md` for the tag, the GitHub Release and the version bump.
