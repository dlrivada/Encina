## Scope reviewed
Read `artifacts\knowledge\stages\archivist.md` and `artifacts\knowledge\issues\54.md`. Issue #54 was closed as not planned on 2025-12-27 with no PR and no commit, so its diff is empty and there is no code to review as a pull request. No scope correction.

Checks run in the audit worktree `D:\Proyectos\Encina\.claude\worktrees\wia-54`:
- Grep `Asp\.Versioning|ApiVersion|AddEncinaVersioning` over `src\`, output mode count: no matches (0 occurrences, 0 files).
- The same pattern over `Directory.Packages.props`, `Directory.Build.props`, `*.csproj` and `tests\**\*.cs`: 0 matches. No package reference and no test refers to it.
- Grep `Version` over `src\Encina.AspNetCore\**\*.cs`: no files. The package has no versioning code at all.

The surface the issue proposed (`[ApiVersion]` on handlers, `AddEncinaVersioning`, deprecation support) therefore does not exist, which is consistent with the rejection. Following the closed-in-error / empty-diff precedent, I did not walk out to other packages beyond these absence checks.

## Findings
- none

## Informational (not findings)
- Backlog contradicts the rejection (already recorded as the `gotcha` entry in `artifacts\knowledge\issues\54.md`, destination backlog `#192`). Open state read through `gh api repos/dlrivada/Encina/issues/<n>` on 2026-10-21:
  - #192 "[FEATURE] API Versioning Helpers" (same title as #54), open, milestone "Post-1.0: Web APIs & Source Generators".
  - #348 "[FEATURE] API Versioning Integration", open, milestone "Post-1.0: Web Advanced & Developer Experience".
  - #307 "[FEATURE] Request Versioning Pattern for API Evolution", open, milestone "Post-1.0: Event Sourcing Advanced & Validation Extensions". It concerns contract evolution of requests, a different mechanism, so it is related but not in conflict.
  - #889 (epic, open) lists #192 as an item.
  There is no code to cite for this, so it is not a code finding with file:line evidence. Whether #192 and #348 should be closed or rewritten to match the recorded decision (versioning at the HTTP layer, no `[ApiVersion]` on handlers) is a backlog decision for the maintainer and the remediation stage, not a code defect. I did not re-read the bodies of #192/#348; the archivist stage describes them, and I only confirmed their open state and milestones.
- The archivist reports that `docs/engineering/PROJECT-HISTORY.md` line 724 still calls #389 and #458 open (they were closed as duplicates on 2026-10-06). That is a documentation item for `docs-reviewer`; I did not re-verify it.

## Siblings audited
- Provider-specific variants: not applicable. #54 is not a provider-dependent feature (no database, cache, transport or lock code) and shipped nothing.
- Same idea elsewhere in the code: `Asp.Versioning|ApiVersion|AddEncinaVersioning` returns 0 matches across `src\` (checked above, whole tree, not only `Encina.AspNetCore`), so no package re-implemented the rejected handler-level versioning.
- Same idea elsewhere in the backlog: #192, #348 (conflicting, open), #307 (related, open), #389 and #458 (closed as duplicates). State read through REST on 2026-10-21 for #192, #348, #307 and #889; #389 and #458 closed state is taken from the archivist stage.

## Lessons for the pipeline
- Closed-as-rejected, empty-diff issue: the code stage reduces to repo-wide absence greps for the proposed API names plus a REST state check of the contradicting open issues; running them took four calls.
- While GraphQL is exhausted, `gh api repos/dlrivada/Encina/issues/<n> --jq` works. In PowerShell, `(.labels|map(.name))-join(",")` inside `--jq` fails with "join cannot be applied to an array including: array ([])"; leave labels out or use `.labels[].name`.
