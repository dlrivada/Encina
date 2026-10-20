# Docs stage, issue #3 (delta rules-2026-10, rule (a) only)

## Pages reviewed

Scope: the issue shipped no page of its own (audit #3 recorded that). The pages that describe what it delivered (`Log.cs` / `[LoggerMessage]` / CA1848 and the EventId governance that followed) were found with a search for `LoggerMessage|CA1848` over `docs/`, the root files and `src/**/*.md`, run from relative paths inside the worktree, then narrowed to pages that state the convention. Plans, release history, knowledge records and audit records (`docs/knowledge/`, `docs/plans/`, `docs/releases/`) are historical and were not reviewed as current docs.

| Page | Role | Rule (a) result |
| --- | --- | --- |
| `docs/architecture/adr/021-eventid-uniqueness-enforcement.md` | Reference/decision for the rule | findings 2, 3 |
| `docs/contributing/README.md` section 3.3 | How-to ("You add logging") | finding 2 |
| `src/Encina.Testing.Architecture/README.md` ("EventId Uniqueness Rules") | Reference with C# sample | sample checked, correct |
| `src/Encina.Hangfire/README.md` ("Logging") | Package README | finding 1 |
| `AGENTS.md` section 7, `docs/engineering/HOW-ENCINA-IS-BUILT.md:178`, `docs/testing/coverage-measurement-methodology.md:165` | Rules and one-line mentions | no finding |
| `src/Encina.Quartz/README.md`, `src/Encina.EntityFrameworkCore/README.md`, `src/Encina.OpenTelemetry/README.md` | Package READMEs with logging words | no `Log.cs` claim; nothing to check |
| 19 of the 23 packages in the record have no README or none mentioning logging: 11 have no README (AmazonSQS, AzureServiceBus, gRPC, InMemory, Kafka, Marten, MQTT, NATS, RabbitMQ, Redis.PubSub, SignalR) and 8 have 0 logging hits (the three ADO and three Dapper, GraphQL and MongoDB READMEs) | | see finding 4 |

Search regex for the per-page table: `LoggerMessage|CA1848` (docs, root, src md) and `Log\.cs|logging|ILogger|EventId|LoggerMessage` per package README. `docs/INVENTORY.md` hits (lines 5887-6170) are per-package feature bullets in Spanish-mixed inventory text and were not reviewed beyond confirming they are descriptions, not samples.

Verified against `src/`: the sample in `src/Encina.Testing.Architecture/README.md:180-222` compiles in principle. `EventIdUniquenessRule.AssertEveryLoggerMessageHasEventId`, `AssertEventIdsAreGloballyUnique`, `AssertEventIdsWithinRegisteredRanges(IReadOnlyList<Assembly>, IReadOnlyDictionary<string, IReadOnlyList<string>>)` and `AssertNoRangeOverlaps()` exist with that shape (`src/Encina.Testing.Architecture/EventIdUniquenessRule.cs:55,102,143,194`); `EventIdRanges.ComplianceGDPR`, `MessagingOutbox`, `MessagingInbox`, `MessagingSaga`, `MessagingScheduling` and `Messaging` exist (`src/Encina/Diagnostics/EventIdRanges.cs:87-126,310`); `GDPROptions` (`src/Encina.Compliance.GDPR/GDPROptions.cs:28`) and `OutboxMessage` (`src/Encina.EntityFrameworkCore/Outbox/OutboxMessage.cs:26`, also in the ADO/Dapper/MongoDB packages; the `Encina.Messaging` assembly name was not separately verified) exist.

## Findings

1. **Major** — `src/Encina.Hangfire/README.md:410-424`, heading "Logging". The sample claims job logs "automatically include ... CorrelationId (from RequestContext)" and shows `// [INF] Executing Hangfire job for request ProcessOrderCommand (CorrelationId: a1b2c3d4)`. The real templates in `src/Encina.Hangfire/Log.cs` (EventIds 4000-4009) are `"Executing Hangfire job for request {RequestType}"`, `"Hangfire job completed successfully for request {RequestType}"` and so on; none has a `CorrelationId` placeholder, and a search for `CorrelationId` over every `.cs` file of `src/Encina.Hangfire` returns no hit. The README sample output and the "automatically include" list do not match the delivered logging (rule (a) point 2: sample correct against `src/`). The failure template also logs `{ErrorCode} ({Classification})`, not "error details".

2. **Minor** — `docs/contributing/README.md:159` (section 3.3 "You add logging") and `docs/architecture/adr/021-eventid-uniqueness-enforcement.md` (Decision sections 1-4). Section 3.3 explains a five-step procedure (check the registry, register a range, create `*LogMessages.cs`, update `PublicAPI.Unshipped.txt`, run architecture tests) as one 712-character paragraph (measured with `Get-Content` line length) plus a second 478-character paragraph; the same steps exist as a numbered list in ADR-021 section 4. A how-to page presents steps as a numbered list, and no snippet shows a registered range or a `[LoggerMessage]` declaration. ADR-021 has no diagram of the flow registry -> `Log.cs` -> architecture test; it has only tables and a bullet list (0 Mermaid blocks in the three pages checked). Visual and scannable check (rule (a) point 1). Placement (rule (a) point 4) also fails for `docs/contributing/README.md`: line 1 is the `# Contributing to Encina: onboarding guide` title, so the page has no just-the-docs front matter (`title`, `parent`, `nav_order`), which `.claude/skills/encina-docs/SKILL.md` section 2 requires on every page under `docs/`, and `docs/_config.yml` has no `contributing` entry (searched) that would exclude it from the site. (#1829 covers a different page.)

3. **Minor** — `docs/architecture/adr/021-eventid-uniqueness-enforcement.md:105-108` (heading "Amendment (2026-09-22, #1120)"). Hand-typed counts: "45 of the 69 packages", "115 EventIds", "one id by 33", "eight were duplicated", "13 ... dead-letter messages", "44 ranges", "754 EventIds". The amendment is dated but names no command that reproduces them. `docs/engineering/assessments/2026-10-05-observability.md:11` already uses the accepted form (measured date and `Select-String` over `src/` and `tests/`, approximate, not coverage figures). Figures cited, never hand-typed (rule (a) point 3). Also `:96` still says "mitigated by CLAUDE.md instructions" although the rules moved to `AGENTS.md` section 7.

4. **Minor** — adequacy of docs for the delivered feature (rule (a) point 5). #3 delivered `[LoggerMessage]` logging in 22 packages. The convention is documented (AGENTS.md section 7, ADR-021, `docs/contributing/README.md` section 3.3), but there is no concept or guide page about structured logging (what a `Log.cs` contains, how to read EventIds, how to filter on them) and no reference of which package owns which EventId range outside the ADR table and `EventIdRanges.cs`. `docs/tutorials/index.md`, `docs/guides/how-to-write-a-pipeline-behavior.md` and `docs/introduction.md` contain no hit for `logging|LoggerMessage|EventId` in the search run for this stage. Per-package READMEs of the transports and data-access packages do not mention logging (11 have no README at all). Minor because the issue is an internal refactor, not a user-facing feature.

## Informational (not findings)

- `docs/architecture/adr/021-eventid-uniqueness-enforcement.md` has no `nav_order`; the sibling ADR pages 020 and 022 have none either (search over `^nav_order`), so placement under `parent: ADRs` is consistent. The ADR index lists 021.
- The ADR range table lists "Free" ranges without `3850-3899`, which `AGENTS.md` section 7 lists as free (it sits inside the table row "Caching and locks 3500-3899"); this is the rule's own dated list, not a #3 matter.
- No emoji or Spanish found on the reviewed pages other than `docs/INVENTORY.md`, which was not reviewed in full.
- ADR-021's CA1848 handling is not on a page in its own right; the suppression rule lives in `AGENTS.md:116` and was not re-checked here (code stage's scope).

## Lessons for the pipeline

- When an issue shipped no page, the delta docs stage can limit itself to the pages that describe its convention and the READMEs of the touched packages; the one concrete defect came from a README sample output compared against the package's own `Log.cs` templates, not from the rule pages.
- `Select-String` has no `-Recurse`; pipe `Get-ChildItem -Recurse` into it (a first attempt failed on that parameter).
