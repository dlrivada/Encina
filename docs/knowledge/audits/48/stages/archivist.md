## Scope
Confirmed: issue #48 touched no code under `src/`, `tests/` or `docs/`. Evidence: the timeline (`gh api .../issues/48/timeline`) has only labeled, milestoned, commented, closed, renamed and project events; no `referenced` commit, no cross-referenced closing PR. `src/Encina.OpenApi` does not exist (`Test-Path` False) and no `src/` project has OpenApi/Swagger in its name. Closed 2025-12-26T22:38:08Z by a manual comment of the owner (created 2025-12-24T13:27:27Z, so 2 d 9 h 10 min).

Scope for later stages is therefore the decision surface only, not code:
- `docs/engineering/PROJECT-HISTORY.md` line 530 (records the rejection).
- `ROADMAP.md` lines 647 and 650, `docs/INVENTORY.md` (lines 6543, 6716, 6724, 6753, 7423), `CHANGELOG.md` line 6759, `docs/releases/v0.11.0/CHANGELOG-DETAILS.md` line 1299, `docs/releases/pre-v0.10.0/README.md` lines 6338 and 6362: all still present `Encina.OpenApi` as planned under #446.
- `docs/INVENTORY.md` line 2319 mentions `AddEncinaOpenApiValidation` (relates to open #231, not this issue).

## Destinations
- Rejection of the Encina.OpenApi wrapper: present in `docs/engineering/PROJECT-HISTORY.md:530` (done). No ADR exists: grep `OpenApi|OpenAPI` in `docs/architecture/adr/*.md` returns no hit. The pre-draft's suggested `adr` and `roadmap` destinations are not present, and its "Rules and lessons" (do not wrap native functionality) is the owner's reasoning for this one case, not a stated general rule; I recorded no rule.
- Missing: ROADMAP.md and INVENTORY.md contradict the rejection (see next section).

## Successor and duplicate issues
- #446 "[FEATURE] OpenAPI/Swagger Integration (Encina.OpenApi)": OPEN (verified with `gh issue view 446`), created 2025-12-28T12:50:49Z, two days after #48 was closed. It re-proposes the same package name (`AddEncinaOpenApi`, `MapEncinaEndpoints`, endpoint generation). It has 0 comments and does not mention #48 or the .NET 10 native rationale. Not a successor in the "implemented" sense: the work is pending and contradicts the rejection. This is a decision conflict for the auditor/remediation stage to surface.
- Related, not duplicates: #198 "OpenAPI 3.1 Enhanced - Schema Generation and SDK Generation" (OPEN, created 2025-12-27) and #231 "Schema/Contract Validation with OpenAPI (Encina.Validation.OpenApi)" (OPEN, created 2025-12-27).

## Lessons for the pipeline
- The worktree had no `artifacts/knowledge/predraft/`; the pre-draft was read from the main checkout (known).
- The pre-draft had `closed_at: 12/26/2025`, `linked_prs: []`, packages `[Encina.OpenApi, Microsoft.AspNetCore.OpenApi]` (neither is shipped code; the record lists none), and ADR/roadmap destinations with no present target; all normalised or dropped.
- A same-name re-proposal after a rejection (#446) is found by `gh issue list --state all --search "<package> in:title"`; the rejected issue's own text never links it.
