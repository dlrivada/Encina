## Scope

Issue #64 (created 2025-12-24T13:32:38Z, closed 2025-12-27T11:59:51Z by the maintainer, state_reason completed, no PR; milestone v0.04.0). The timeline `closed` event has no commit; commit `a406152f4` ("feat(contentrouter): add content-based router pattern (Fixes #64)", 2025-12-27 13:01 +0100) is a later `referenced` event and is the delivering commit (21 files, 3806 insertions).

Source today (all exist, `src/Encina.Messaging/ContentRouter/`): `IContentRouter.cs`, `ContentRouter.cs`, `ContentRouterBuilder.cs`, `RouteDefinition.cs`, `ContentRouterResult.cs`, `ContentRouterOptions.cs`, `ContentRouterErrorCodes.cs`. Wiring: `src/Encina.Messaging/MessagingConfiguration.cs` (`UseContentRouter`, line 197), `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` (line 146-155, `AddScoped<IContentRouter, ContentRouter.ContentRouter>()`), reused by `Encina.EntityFrameworkCore` (README line 412). Package `Encina.Messaging` only (the issue's affected package; no other package shipped).

Tests today: `tests/Encina.UnitTests/Messaging/ContentRouter/` (ContentRouterTests, ContentRouterBuilderTests, ContentRouterOptionsTests, ContentRouterResultTests, RouteDefinitionTests), `tests/Encina.GuardTests/Messaging/ContentRouter/ContentRouterGuardTests.cs`, `tests/Encina.BenchmarkTests/Encina.Benchmarks/ContentRouterBenchmarks.cs`, plus flag tests in `tests/Encina.UnitTests/Messaging/MessagingConfigurationPatternFlagsTests.cs`. The commit's contract, property, integration and load test files were deleted by consolidation commit `658263027` (verified with `git show --stat`; no replacements by those names). The close comment's "117 tests (unit, integration, property, contract, guard, load)" is therefore not the current count.

Docs: `src/Encina.Messaging/README.md` (UseContentRouter section, line ~410), `ROADMAP.md` line 327 lists Content-Based Router #64 as done, `CHANGELOG.md`. `docs/history/2025-12.md` named in the close comment does not exist in this worktree (history pages were reorganised; not a regression of this issue).

Related open issues found by search: #3412 (SPIKE: lists `ContentRouterOptions.EnableRouteCaching` and `MaxCacheSize` among options nothing reads), #3723 (DEBT: content router errors declare an ErrorCategory), #1580 (raw exception messages in Messaging components), #561 (benchmarks for Messaging orchestrators). No code was removed on purpose.

## Destinations

- Router API (`IContentRouter.RouteAsync`, builder with When/RouteTo/WithPriority/WithMetadata/Default): present in `src/Encina.Messaging/README.md` and covered by `ContentRouterTests.cs` / `ContentRouterBuilderTests.cs` (docs + regression-test, present).
- DI through `MessagingConfiguration.UseContentRouter`: present in README and `MessagingConfigurationPatternFlagsTests.cs` (present).
- Route caching (a Proposed Solution bullet and task "Optimize with route caching"): missing. `EnableRouteCaching` / `MaxCacheSize` are declared with docs claiming expression caching, but a Grep of `src/` finds no reader, `ContentRouter.cs` has no cache, and the only references are `ContentRouterOptionsTests.cs` default/setter checks. Tracked by backlog #3412 (OPEN).
- `AddEncinaContentRouter<T>(...)` and `IEncina.Route(order)` from the issue body: missing; Grep over `src/` finds neither. The close comment describes a different API (UseContentRouter + IContentRouter), so the body sample was aspirational. Not tracked by any issue I found; recorded as a `current: no` gotcha.
- Contract/property/integration/load tests promised in the close comment: removed by consolidation; no current equivalent. No ADR exists for the pattern (not searched beyond the ADR index naming; not needed for the record).
- Pre-draft items dropped: "Rules and lessons" (85% coverage, test types, phase/priority) were acceptance criteria or labels, not decisions; `linked_prs: [unknown]` and `closed_at: 12/27/2025` normalised (no PR; closed 2025-12-27).

## Successor and duplicate issues

Not rejected, superseded or duplicate; no successor. Follow-ups (state verified with `gh api`): #3412 OPEN (unread options incl. the two caching options), #3723 OPEN (ErrorCategory for content router errors).

## Lessons for the pipeline

- A close comment that lists a fluent API and DI flag can hide that body-promised pieces (route caching, `AddEncinaContentRouter`, `IEncina.Route`) never shipped; options that exist but are read nowhere (found via the open spike #3412 list) are a quick tell for a "documented but not implemented" feature. (issue-archivist: Grep every option of a delivered Options class for a reader in src/ before accepting the feature bullet it represents.)
- GraphQL quota was unreliable; `gh api repos/.../issues/<n>`, `/comments` and `/timeline` with `--jq` gave everything needed, and the pre-draft is read from the main checkout (absent in the worktree).
