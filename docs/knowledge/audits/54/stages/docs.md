## Pages reviewed

Issue #54 was closed as not planned on 2025-12-27 with no PR and no code (code stage: 0 matches for `Asp.Versioning|ApiVersion|AddEncinaVersioning` in `src/`). The docs that record or contradict the decision were reviewed:

- `docs/engineering/PROJECT-HISTORY.md:524` (decision bullet) and `:724` (ADR candidate A6).
- `CHANGELOG.md:8764` (Removed entry).
- `docs/releases/v0.11.0/CHANGELOG-DETAILS.md:3307` (same entry) and the entries it contradicts at `:1467`, `:1531-1535`, `:2036-2037`, `:2794-2796`.
- `docs/INVENTORY.md:4279` and `:4303` (status table), plus `:2834`, `:5381`, `:6837` (backlog rows).
- `docs/roadmap-documentacion.md:523`, `docs/architecture/adr/index.md`.

Decision text checked against the close comment of #54 (read with `gh api repos/dlrivada/Encina/issues/54/comments`): the claim "versioning belongs to the HTTP layer, handlers carry no `[ApiVersion]`" at PROJECT-HISTORY.md:524, :724 and CHANGELOG.md:8764 matches it. No hand-typed figures, no identifiers needing a `src/` match (the only code names are the rejected ones, absent by design).

Issue states verified on 2026-10-21 with `gh api repos/dlrivada/Encina/issues/<n>`:

| Issue | State | Detail |
| --- | --- | --- |
| #54 | closed, not_planned | the rejection |
| #389 | closed, duplicate | last comment: duplicate of #348, `IApiVersionContext` carried into #348 |
| #458 | closed, duplicate | last comment: duplicate of #348, version discovery endpoint carried into #348 |
| #192 | open | `[FEATURE] API Versioning Helpers` (same title as #54) |
| #348 | open | `[FEATURE] API Versioning Integration` |

## Findings

1. **Minor** — `docs/engineering/PROJECT-HISTORY.md:724` (ADR candidate A6, column "Note") says "Relevant to the open #389 and #458". Both were closed as duplicates of #348 on 2026-10-06 (REST state `closed`, reason `duplicate`, verified). The check that fails is accuracy against today's state. The note should point to the open issues that still propose handler-level versioning, #192 and #348 (the surviving target of both duplicates).

2. **Major** — `docs/engineering/PROJECT-HISTORY.md:524` and `:724` (A6) record "versioning belongs to the HTTP layer, not to handler routing" as a decision, but the decision has no ADR (`docs/architecture/adr/index.md` has no versioning entry; searched "ersioning", 0 hits) and the backlog still contradicts it: #192 (Asp.Versioning.Http integration with version-aware handler registration) and #348 (versioned handlers routed by requested version) are open and sit in Post-1.0 milestones, #889 lists #192. The decision check fails: a rejection contradicted by open plans, with no superseding or recording ADR, so a later worker can pick #192 or #348 and rebuild what #54 rejected. A6 is still an unpromoted candidate (PROJECT-HISTORY.md says nothing there is a rule until the maintainer promotes it). Resolution needs the maintainer: promote A6 to an ADR and close or rewrite #192 and #348 to the HTTP-layer scope.

3. **Minor** — `docs/INVENTORY.md:4279` (`| **API Versioning** | 📋 | #389 - Version-aware handlers |`) and `:4303` (`| **API Versioning** | 🔄 Planned | Encina.ApiVersioning (#458) |`) present handler versioning as planned, citing #389 and #458, which are closed duplicates, while the project decision (#54) rejects it. The page is dated 2 February 2026 (line 3) and has no snapshot or frozen banner in its first 12 lines, so it reads as current. The backlog rows at `:2834`, `:5381` and `:6837` carry the same idea. Fix together with finding 2, once the maintainer decides what #192 and #348 become.

4. **Minor** — `docs/releases/v0.11.0/CHANGELOG-DETAILS.md` contradicts itself: `:3307` records #54 as "Closed as won't fix ... `[ApiVersion]` to CQRS handlers would be redundant", while `:1467-1468` (#458, `[ApiVersion("1.0")]` for handlers), `:1531-1535` (#389, `ApiVersioningPipelineBehavior` for version-aware handler routing), `:2036-2037` (#348) and `:2794-2796` (#192, `[ApiVersion]` support for handlers) list the same idea as planned. The release page is historical, so the fix is a dated note on those entries naming the decision (and that #389 and #458 are duplicates of #348), not a rewrite. `CHANGELOG.md:8764` itself is accurate and needs no change.

5. **Minor** — rule (a) point 5, adequate docs. The decision tells readers to use `Asp.Versioning` on controllers and call `Send` from the versioned endpoint, but no guide teaches that pattern: `docs/roadmap-documentacion.md:523` has `- [ ] **API Versioning**` unticked, and the only descriptions of the pattern are in the #54 close comment and the PROJECT-HISTORY bullet. The decision is therefore findable only in history pages. A short how-to under the web guides (versioned endpoint calling `IEncina.Send`, handler unversioned) would give the decision a reader-facing home; Diátaxis quadrant: how-to, reader: developer exposing a versioned HTTP API.

## Informational (not findings)

- `CHANGELOG.md:8764` and `CHANGELOG-DETAILS.md:3307` word the outcome as "Closed as 'won't fix'"; GitHub's reason is `not_planned`. Same meaning, no action.
- `docs/INVENTORY.md` is written in Spanish (for example `:2834`, `:5474-5479`). The Spanish-is-a-blocker rule applies to pages in review; this page is outside the issue's scope and the language drift is not caused by #54, so it is noted here and not counted.
- The archivist and code stages already name #192, #348 and #307. #307 (request versioning for contract evolution) is a different mechanism and is not in conflict with the decision.
- Lychee and markdownlint were not run: nothing was edited, and the pages in scope are historical records.

## Lessons for the pipeline

- For a rejected idea, grep `docs/` for the issue numbers of its backlog twins (here #389, #458, #192, #348) as well as the idea's name: the contradicting status rows were in `INVENTORY.md` and the v0.11.0 release page, not in the pages the archivist listed.
- When a note says "open #N", check #N's state and, if it closed as a duplicate, follow the last comment to the surviving issue; the fix points to the target (#348), not just a deletion.
