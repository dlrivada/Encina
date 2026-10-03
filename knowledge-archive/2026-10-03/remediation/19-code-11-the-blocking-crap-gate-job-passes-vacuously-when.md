<!--
title: [BUG] The blocking crap-gate job passes vacuously when no Cobertura coverage file was downloaded
labels: bug
milestone: v0.14.0 — Hardening
kind: bug
-->

## Description

Reported by: code 11, tests 2.

The blocking `crap-gate` job passes without checking anything when no coverage was collected. In `.github/workflows/ci.yml:538-541` the "Run CRAP gate" step prints "No Cobertura coverage files found among the downloaded artifacts; nothing to gate." and runs `exit 0` when no `coverage.cobertura.xml` was downloaded. Every flag job uploads its results with `if-no-files-found: ignore` (`ci.yml:171`, `:245`, `:284`, `:323`, `:362`, `:472`), so a PR whose collector wrote no report, or whose upload failed, is waved through with zero data.

The job comment (`ci.yml:491-492`) says the self-test is there so that "a broken gate fails loudly instead of silently passing every PR". AGENTS.md section 3 requires gates to fail closed: a gate that cannot verify must deny.

`crap-gate.cs` itself already denies this case: `.github/scripts/crap-gate.cs:64-68` returns 2 when no Cobertura file is given, and `:70-77` returns 2 for a missing file. The vacuous pass lives only in the workflow shell snippet, which the self-test (`.github/scripts/crap-gate-selftest.ps1`) never reaches. The self-test has no assertion for exit 2 on no input or on a missing file either (its five assertions all pass real fixtures).

## Steps to Reproduce

1. Open a pull request whose diff touches `src/**/*.cs`, so the `crap-gate` job runs (`ci.yml:499-504`).
2. Let the flag jobs finish without a `coverage.cobertura.xml` reaching any `test-results-*` artifact (the collector writes no report, or the upload fails; the uploads ignore a missing file).
3. The `crap-gate` job downloads the artifacts (`ci.yml:526-531`) and the `find` at `:537` returns no files.
4. The step takes the branch at `:538-541` and exits 0; the job is green.

## Expected Behavior

When the diff touches `src/**/*.cs` and no Cobertura file exists, the job fails. Removing the `exit 0` shortcut lets the script's own exit 2 (or a diff-aware "src changed, no data" failure) apply.

## Actual Behavior

The job prints "nothing to gate." and succeeds, so the blocking CRAP rule is not applied to that PR.

## Environment

- **Encina Version**: 0.14.0-dev
- **.NET Version**: .NET 10
- **OS**: Not applicable (found by static review of the code, not at runtime)
- **Package(s) Affected**: none (CI workflow `.github/workflows/ci.yml`)

## Code Sample

```yaml
          mapfile -t cobertura_files < <(find artifacts/crap-gate/coverage -type f -name 'coverage.cobertura.xml')
          if [ ${#cobertura_files[@]} -eq 0 ]; then
            echo "No Cobertura coverage files found among the downloaded artifacts; nothing to gate."
            exit 0
          fi
```

## Stack Trace

```
Not applicable: the step exits 0 without an error.
```

## Additional Context

Tests to add with the fix, all deterministic (file inputs and exit codes, no network, no runner needed):

- In `.github/scripts/crap-gate-selftest.ps1`: an assertion that `Invoke-CrapGate -GateArgs @('--enforce', '--diff', 'sample.diff')` exits 2, and one with a nonexistent Cobertura path that also exits 2.
- A workflow-text test in the style of `tests/Encina.UnitTests/Workflows/WorkflowTemplateTests.cs`: the `crap-gate` job's run step must not contain an `exit 0` guarded by an empty `cobertura_files`.
- The end-to-end "all uploads empty" case cannot be reproduced locally (it depends on `actions/download-artifact`), so test the pieces, not the job.

Related known limits of the gate: #1355 (diff parsing and exemption matching) and #1360 (methods dropped for files with no obligations).

Related Issues:

- #19 (This issue)
- #1355 - known limits of the gate
- #1360 - known limits of the gate
