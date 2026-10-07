## Pages reviewed

Delta `rules-2026-10`, rule (a) only. Issue #26 shipped nothing (duplicate of #42), so the scope is the destination of the feature it proposed (Dead Letter Queue, code in `src/Encina.Messaging/DeadLetter/`, 13 `.cs` files):

- `src/Encina.Messaging/README.md` (636 physical lines): the only README that mentions the DLQ.
- `docs/messaging/index.md`, `docs/features/index.md`, `docs/tutorials/index.md`, `docs/tutorials/quickstart.md`, `docs/index.md`: checked for a DLQ page, link or tutorial entry.
- `docs/features/` has no page named for dead letters (`Get-ChildItem docs\features -Filter *dead*` returns nothing).

Search: `Select-String` for `IDeadLetter|DeadLetterManager|DeadLetterOptions|AddEncinaDeadLetter|dead.letter` over `docs/**/*.md`, `README.md` and the Messaging README. Pages that teach the feature: 0. Passing mentions: `docs/features/scheduling.md:62,78,101` (scheduled-message retry, a different mechanism), `docs/messaging/transports.md:220`, ADR-029 (recoverability). The real API is named only in `docs/INVENTORY.md:434-437` (type list).

## Findings

1. **Major** — Rule (a) point 5, no adequate docs for the delivered feature. `IDeadLetterStore`, `IDeadLetterManager`, `IDeadLetterMessageFactory`, `DeadLetterOrchestrator`, `DeadLetterOptions`, `DeadLetterCleanupProcessor`, the health check and `AddEncinaDeadLetterQueue<TStore, TFactory>` (`src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs:20` and `:65`) have no concept or guide page, no option or error-code reference and no tutorial entry. `Select-String` over `docs/messaging/index.md`, `docs/features/index.md`, `docs/tutorials/index.md`, `docs/tutorials/quickstart.md` and `docs/index.md` for `DeadLetter|Dead Letter` returns 0 hits, and `src/Encina.Messaging/README.md` has no DLQ section. Related open issues: #583, #584, #585 (persistent stores) and #771 (cleanup processor) do not ask for documentation; none covers the page. Suggested destination: a `docs/features/dead-letter-queue.md` page (reference plus how-to to replay and purge) linked from `docs/features/index.md` and `docs/messaging/index.md`, and an entry in the messaging tutorial or learning path.

2. **Major** — Rule (a) point 2, C# sample not correct against `src/`. `src/Encina.Messaging/README.md:214-220` (Recoverability section) sends failed messages with `_deadLetterQueue.SendAsync(failedMessage, ct)`. No `_deadLetterQueue` field or `SendAsync` exists on the DLQ API: `IDeadLetterManager` exposes `ReplayAsync`, `ReplayAllAsync`, `GetMessageAsync`, `GetMessagesAsync`, `GetCountAsync` and `GetStatisticsAsync` (`src/Encina.Messaging/DeadLetter/IDeadLetterManager.cs`), and the sample never shows `AddEncinaDeadLetterQueue`. The sample teaches an invented hand-wired DLQ instead of the shipped one; `OnPermanentFailure` and `FailedMessage.TotalAttempts` do exist (`DelayedRetryProcessor.cs:334`, `FailedMessage.cs:51`). Nearby: the same block opens with `services.AddEncinaMessaging(config => ...)`, which lesson #16 found to be only an XML-comment identifier; that is tracked under the #16 delta (#1886) and not repeated here.

3. **Minor** — Rule (a) point 1, tone and no emojis. `src/Encina.Messaging/README.md` has emojis on 15 lines: the headings at :12, :35 and :46, and the status checklist at :621-632 (12 consecutive lines using the check-mark emoji as a bullet). Count command: `Select-String` with the range `[☀-➿⏳⭐]|[\uD83C-\uD83E]` returns lines 12, 35, 46 and 621 to 632. Already tracked by open #1886 ("Remove emojis from the Encina.Messaging README headings and roadmap checklist"); no new issue needed.

4. **Minor** — Rule (a) point 1, visual and scannable. `src/Encina.Messaging/README.md` has 0 Mermaid blocks (`Select-String '```mermaid'`); the Recoverability section (heading at :182) explains immediate retry, delayed retry and the permanent-failure callback in prose and code only. A Mermaid flowchart of immediate retry, delayed retry and dead letter would show the structure. Minor, since the DLQ part is one callback. No open issue covers it.

## Informational (not findings)

- `docs/INVENTORY.md:370` has the Spanish phrase "Manejo de fallos permanentes" in the DLQ row (a language-rule issue, outside rule (a); `docs/INVENTORY.md` is also excluded from markdownlint by the SKILL). Worth passing to the INVENTORY owner.
- Rule (a) points 3 (figures) and 4 (placement) do not apply: #26 shipped no page, and the README sample carries no coverage, mutation or performance figure.
- Not run: lychee and markdownlint (delta mode checks only rule (a)).

## Lessons for the pipeline

- For a closed-in-error issue whose feature a successor delivered (#42), the delta docs stage still runs point 5 on the delivered package folder: list the `src/` types, then search `docs/` for each; here only a type list in INVENTORY names them. The sample defect came from the one README that mentions the feature (a `_deadLetterQueue.SendAsync` that matches no type), found by comparing the sample with the real `IDeadLetterManager` members.
