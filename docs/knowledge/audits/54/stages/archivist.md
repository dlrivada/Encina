## Scope
Issue #54 (created 2025-12-24T13:29:22Z, closed 2025-12-27T12:12:28Z as not planned, 2 d 22 h 43 min) was rejected with a reasoned comment. No PR, no commit (timeline: labeled, milestoned, commented, closed, later labeled/renamed/project events only). Nothing shipped, so there is no code to audit.

Verified absences today:
- `src/` has zero matches for `Asp.Versioning` or `ApiVersion` (Grep count 0); `src/Encina.AspNetCore` exists but has no versioning code and no `AddEncinaVersioning`.
- The issue's proposed surface (`[ApiVersion]` on handlers, `AddEncinaVersioning`, deprecation support) therefore does not exist, consistent with the rejection.

Scope for the code stage (replacement/enforcement path): none beyond confirming absence in `src/Encina.AspNetCore`; docs scope is `docs/engineering/PROJECT-HISTORY.md` (lines 524 and 724), `CHANGELOG.md` line 8764 and `docs/releases/v0.11.0/CHANGELOG-DETAILS.md` line 3307. Pre-draft claim "packages: Encina.AspNetCore" came from the unticked Affected Packages; the record lists no package.

## Destinations
- Decision "versioning is an HTTP-layer concern, handlers carry no version": present in `docs/engineering/PROJECT-HISTORY.md` line 524 and decision table A6 (line 724, "Relevant to the open #389 and #458"; both are now closed as duplicates, so that note is stale), and in `CHANGELOG.md` line 8764 ("Closed as won't fix"). Present.
- Rejected alternative (handler-level `[ApiVersion]`/`AddEncinaVersioning`): present in CHANGELOG.md line 8764. Present.
- The pre-draft's "claude-md" candidate destination was dropped: no source states it as a rule for AGENTS.md. `docs/roadmap-documentacion.md` line 523 has an unticked "API Versioning" docs item; no versioning guide page exists as a destination (no ADR either: grep for `Asp.Versioning` and `API versioning` in all *.md found only changelog, history, roadmap and pre-v0.10.0 backlog text).

## Successor and duplicate issues
No successor was named by the close comment. Searching the repo found contradicting open backlog items (state verified with `gh api repos/dlrivada/Encina/issues/<n>`):
- #192 "[FEATURE] API Versioning Helpers" (identical title), OPEN, milestone Post-1.0: Web APIs & Source Generators. Its body asks for `Asp.Versioning.Http` integration and version-aware handler registration: the exact idea #54 rejected. Epic #889 lists it as item 9.
- #348 "[FEATURE] API Versioning Integration", OPEN, Post-1.0 Web Advanced & DX. Versioned handlers routed by requested version.
- #307 "[FEATURE] Request Versioning Pattern for API Evolution", OPEN (contract evolution for requests, related but a different mechanism).
- #389 and #458 (handler versioning pipeline behavior / `Encina.ApiVersioning`): CLOSED as duplicate on 2026-10-06; PROJECT-HISTORY A6 still calls them open.
So the rejection is not reflected in the backlog: the work is pending post-1.0 and conflicts with the decision.

## Lessons for the pipeline
- A rejection of a design idea needs a body search of the backlog for the same idea under other titles (#192 has the identical title, #348 and #307 differ); the pre-draft found none of them because it only had issue text.
- The pre-draft listed the unticked "Affected Packages" entry, invented a claude-md destination, and left dates and `linked_prs` in non-schema form; normalised or dropped.
- While GraphQL quota is exhausted, `gh api repos/.../issues/<n>` plus `/comments` and `/timeline --jq` work; `gh issue list` fails, `gh api search/issues` works.
