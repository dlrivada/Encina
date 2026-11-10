---
title: "ADR-063: An In-Repository Coverage Comment Replaces Codecov"
layout: default
parent: ADRs
grand_parent: Architecture
---

# ADR-063: An In-Repository Coverage Comment Replaces Codecov (Accepted)

## Status

**Accepted** - 2026-11-10, by the maintainer's decision of 2026-11-05 to replace Codecov entirely. Issue: [#3289](https://github.com/dlrivada/Encina/issues/3289). Supersedes [ADR-023](023-coverage-strategy-codecov-sonarcloud.md).

[AGENTS.md](../../../AGENTS.md) section 8 requires an ADR for architectural decisions; the choice of coverage tooling is one.

## Context

[AGENTS.md](../../../AGENTS.md) section 9 measures coverage per flag (unit, guard, contract, property, integration) against the per-file targets in `.github/coverage-manifest/`. There is no project-wide percentage. Codecov reports project and patch percentages, which answer a different question and are unrelated to those targets.

Codecov also caused concrete harm:

- Coverage uploads from fork pull requests fail, so a fork PR showed a false `codecov/patch` result ([PR #3181](https://github.com/dlrivada/Encina/pull/3181)).
- Waiting for `codecov/patch` to leave the pending state delayed merges ([PR #3248](https://github.com/dlrivada/Encina/pull/3248)).

The gates that matter do not depend on Codecov: `crap-gate`, `target-ratchet` and the per-file targets computed by `.github/scripts/coverage-report.cs`. The [coverage dashboard](https://dlrivada.github.io/Encina/coverage/) on GitHub Pages is unaffected, and SonarCloud stays a static-analysis service only (ADR-023 decision 2).

## Decision

Codecov is removed. A workflow in this repository posts one informational comment per pull request with the "Per-file targets" table of the `src/` files the pull request touches.

Two workflows, split at the trust boundary:

1. **`ci.yml`** (event `pull_request`, read-only token, fork pull requests included) already uploads the `test-results-*` artifacts that hold the Cobertura data. A new job, `coverage-comment-input`, uploads the artifact `coverage-comment-pr` with the pull request number and the head SHA.
2. **`.github/workflows/coverage-comment.yml`** is triggered by `workflow_run` on CI and runs only base-repository code. It has two jobs, with permissions declared per job ([AGENTS.md](../../../AGENTS.md) section 10):
   - `report` (permissions `actions`, `contents` and `pull-requests`, all read):
     - downloads the artifacts as untrusted data;
     - accepts the pull request number only if the API says that pull request is open with exactly the run's head SHA, head repository and head branch (the run's `pull_requests` array is empty for fork pull requests, so the API is the only source for them);
     - reads the touched-file list from the API;
     - reads the pull request head's coverage-manifest JSON blobs with `git show`, never checking out or running pull request code;
     - runs `coverage-report.cs --files-from` and uploads the rendered Markdown.
   - `comment` (the only job with `pull-requests: write`, no checkout) posts or updates one comment, identified by the hidden marker `<!-- encina-coverage-comment -->`.

## Alternatives considered

- **Keep Codecov**: rejected for the reasons in Context.
- **`pull_request_target` with a write token**: rejected because it puts a context with secrets and a write token next to data from a fork.
- **Post the comment from the `pull_request` job**: rejected because the token of a fork pull request is read-only.

## Consequences

- The comment is informational and never a gate. `coverage-report.cs` reports target attainment without an exit code, and `target-ratchet` only checks changes to targets, so reviewers check the attainment against the targets manually. `crap-gate` remains the blocking gate for the methods a pull request touches.
- Pull request CI measures only the flags whose inputs the pull request touches. It does not run the Observability integration shard nor the SQLite EF Core job that CI Full runs. The table says "no data" only when the file and the flag have no recorded lines; a percentage can still exclude an omitted shard (a shard that did not run in pull request CI), so a measured value is not proof that every shard ran.
- The Cobertura data comes from the pull request, so a malicious pull request could falsify its own numbers in an informational comment. The gates (`crap-gate`, `target-ratchet`) run in the pull request's own CI, as before.
- A `workflow_run` workflow only runs from the default branch, so the comment appears only after this change is merged.
- After the merge the maintainer removes the Codecov GitHub app and the `CODECOV_TOKEN` secret.

## Related

- [ADR-023](023-coverage-strategy-codecov-sonarcloud.md), superseded
- [Coverage measurement methodology](../../testing/coverage-measurement-methodology.md)
- [Coverage dashboard](https://dlrivada.github.io/Encina/coverage/)
- Issue: [#3289](https://github.com/dlrivada/Encina/issues/3289)
- Pull requests: [#3181](https://github.com/dlrivada/Encina/pull/3181), [#3248](https://github.com/dlrivada/Encina/pull/3248)

## Date

2026-11-10
