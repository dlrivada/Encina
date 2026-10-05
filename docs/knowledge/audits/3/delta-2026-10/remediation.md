Remediation for #3:
- tests 1 (Minor): draft 3-delta-2026-10-tests-1-github-coverage-manifest-encina-amazonsqs-azureservicebus-ka.md
- tests 2 (Minor): draft 3-delta-2026-10-tests-2-github-coverage-manifest-encina-entityframeworkcore-json-ent.md
- tests 3 (Minor): draft 3-delta-2026-10-tests-3-github-coverage-manifest-encina-ado-sqlserver-ado-postgresql.md
- tests 4 (Minor): draft 3-delta-2026-10-tests-4-github-coverage-manifest-encina-mongodb-json-entries-outbox.md
- tests 5 (Minor): draft 3-delta-2026-10-tests-5-github-coverage-manifest-encina-hangfire-json-entries-hangfi.md
- tests 6 (Minor): draft 3-delta-2026-10-tests-6-github-coverage-manifest-encina-signalr-json-entries-mediato.md
- docs 1 (Major): draft 3-delta-2026-10-docs-1-src-encina-hangfire-readme-md-410-424-heading.md
- docs 2 (Minor): draft 3-delta-2026-10-docs-2-docs-contributing-readme-md-159-section-3-3.md
- docs 3 (Minor): draft 3-delta-2026-10-docs-3-docs-architecture-adr-021-eventid-uniqueness-enforcement-md.md
- docs 4 (Minor): draft 3-delta-2026-10-docs-4-adequacy-of-docs-for-the-delivered-feature-rule.md

## Lessons for the pipeline
- The tests stage matched a file to its tests by file name, not by the declared type: `MediatorHub.cs` declares `EncinaHub` and is tested by `tests/Encina.UnitTests/SignalR/EncinaHubTests.cs`, which the stage reported as absent (tests finding 6). Locate tests with one search for the declared type name before writing "no test file".
- The tests stage called `SignalRNotificationBehavior.cs` a "behavior" (tests finding 6); the file declares `SignalRBroadcastHandler<TNotification>`, a notification handler. Likewise `GraphQLMediatorBridge.cs` declares `GraphQLEncinaBridge`. Read the type declaration before describing what a file is.
- Tests finding 3 justified the EF Core `Outbox/OutboxProcessor.cs` as only forwarding its constructor; the file also overrides `ResolveOutboxStore` (`src/Encina.EntityFrameworkCore/Outbox/OutboxProcessor.cs:42`), which is why it has three coverable lines instead of two.
- The test class named in tests finding 2 for `MartenAggregateRepository.cs` is the file `IAggregateRepositoryContractTests.cs`, which declares the class `MartenContractTests` (`tests/Encina.ContractTests/Marten/Core/IAggregateRepositoryContractTests.cs:16`); name the class, not the file.
- Tests finding 1 tied the NATS target to open issue #1629 although its own measurement shows `NATSMessagePublisher.cs` at 89/89 with lines 93-141 executed; the tests-1 draft does not cite #1629, and the orchestrator can close it with a comment citing that measurement. #1627 (MQTT) is cited in the draft only for the 44 lines uncovered today (108-129, 142-163, 220-227, 307-314).
- `docs/contributing/README.md` has no just-the-docs front matter (first line is the `#` title), which `encina-docs` SKILL section 2 requires on every page under `docs/`; the docs-2 draft carries it as a third item beside the section 3.3 and ADR-021 readability work, so no separate skip reason applies.
- The audit worktree's `Glob` returned no results for paths that exist (`src/Encina.Hangfire/README.md`) and for the drafts just written in the main checkout; `Grep` found them. An absent-path claim needs a second check (Grep or Encina.slnx).
- A root-cause sentence about why a doc was written or left stale needs a date or a quoted source; the docs-3 and docs-4 drafts say "not established" where the stage text gives none, and docs-2 quotes line 3 of the guide.
