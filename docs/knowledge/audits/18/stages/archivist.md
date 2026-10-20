## Scope

Issue #18 is a SPIKE that ended in a "no-go" decision (no Strategy pattern for messaging transports) plus a documentation deliverable. There is no closing PR with code changes; the only referenced commit is `6a72163a1a61c0f52a1d8e2e38a49fd6931af5a9` ("docs: add comprehensive Messaging Transports documentation"), which touched only:

- `docs/messaging/transports.md` — created, still present today (569 lines at commit time; 14,986 bytes today), still has the decision flowchart (4 mermaid diagrams), transport comparison table, per-transport guides and FAQ.
- `docs/messaging/index.md` — updated with a "Messaging Transports" section and a link to `transports.md`; still present with both today.
- `docs/messaging/toc.yml` — updated at the time, but this file no longer exists anywhere under `docs/`: it was removed in commit `5d8b6d3203a2139a01e84c9b15215214c01b2535` ("docs: migrate to just-the-docs theme with proper navigation (#916)", 2026-03-27), a broader docs-platform migration unrelated to #18. Its role is now served by front matter (`layout: default`, `parent: "Messaging"`) in each page, verified present in `transports.md`. Nothing further to scope for `toc.yml`: it was a deliberate, project-wide removal, not a regression of #18's work.

No source code (`src/`) was touched by this issue. The 10 packages the issue named or that exist today for messaging transports (`Encina.RabbitMQ`, `Encina.Kafka`, `Encina.NATS`, `Encina.AzureServiceBus`, `Encina.AmazonSQS`, `Encina.Redis.PubSub`, `Encina.InMemory`, `Encina.MQTT`, `Encina.gRPC`, `Encina.GraphQL`) all still exist under `src/` today, each as an independent package — consistent with the decision (verified with `Get-ChildItem src`). The issue explicitly scoped out "Full implementation of all strategy-specific orchestrators" and "gRPC/GraphQL categorization" — both remain out of scope by design, not by omission.

## Destinations

| Decision | Destination | Status |
| --- | --- | --- |
| No Strategy pattern for transports; each transport package keeps its own full API | `docs/messaging/transports.md` (FAQ: "Should I use a unified messaging interface? No.") | present |
| Same decision, as an ongoing engineering rule | `AGENTS.md` §5 Providers, "Transports (10 + 6 planned)" row — lists all 10 as independent packages that must stay behaviorally coherent (Send/Publish, subscriptions, DLQ, metadata), with no shared strategy interface required | present |
| NATS: one package, two explicit modes (Core / JetStream) | `docs/messaging/transports.md` NATS section | present, but the documented method names (`AddEncinaNatsCore`, `AddEncinaNatsJetStream`) do not exist in `src/Encina.NATS/ServiceCollectionExtensions.cs`, which only exposes `AddEncinaNATS(Action<EncinaNATSOptions>)` with a `UseJetStream` boolean option — **doc/code drift**, filed as a remediation item in the knowledge record |
| No runtime strategy switching; use `Encina.InMemory` for tests | `docs/messaging/transports.md` "Testing with InMemory" section | present |
| Transport choice is per-application, not per-module/per-message | `docs/messaging/transports.md` | present |
| gRPC/GraphQL excluded, classified as "API Bridge" | `docs/messaging/transports.md` (category table, dedicated guides) | present |
| Rejected Options A/B/C (three strategy interfaces; single `IMessageTransport`; persistent/ephemeral split) | No ADR; rationale condensed into `docs/messaging/transports.md`'s FAQ | present in condensed form, not the full A/B/C breakdown from the issue — acceptable, since the issue's own deliverables checklist made an ADR optional and this was a documentation-only outcome |
| Deliverable checklist item "ADR document in docs/architecture/adr/" | none | not created; no ADR records this decision. Evidence: `Select-String` (case-insensitive) over all 32 `docs/architecture/adr/*.md` for `IMessageBrokerStrategy`, `IEventStreamingStrategy`, `IMessageTransport`, `Strategy pattern`, `messaging transport` and `#18\b` returned 0 matches each. (The bare words "transport" and "strategy" do match in many ADRs, for example "strategy" in the titles of ADR-002/003/007/008/011/023 and "transport" in ADR-014/018/021/028/029/030, but those concern other topics, not messaging-transport strategy.) Consistent with a rejected/documentation-only spike where the checklist item was optional, not a gap |

## Successor and duplicate issues

None. The issue's outcome is `rejected-reasoned`, not superseded/duplicate — the decision stands on its own and was not deferred to a later issue. The issue lists three *related* issues (#16 Sagas, #17 Event Sourcing, plus #13/#14 for Caching/Validation orchestrators) as precedent for the same "separate and document" pattern, not as successors of #18 itself; no verification of their state was needed for this record since they are not claimed as carrying forward #18's own decision.

## Lessons for the pipeline

- A commit referenced in an issue's timeline (`referenced` event with a `commit_id`, not a PR) is a valid and sometimes the *only* source of "what shipped" for a documentation-only spike; `gh api .../timeline` surfaces it even when there is no linked PR, and `git show --stat <oid>` is enough to verify its file list.
- Grepping the actual source (`src/Encina.NATS/ServiceCollectionExtensions.cs`) against a documented code sample (`docs/messaging/transports.md`'s NATS section) caught a real doc/code drift: the documented `AddEncinaNatsCore`/`AddEncinaNatsJetStream` extension methods do not exist; only `AddEncinaNATS` with a `UseJetStream` option does. A pre-draft or issue text describing a two-method API should always be checked against the actual `ServiceCollectionExtensions.cs`, not assumed from the issue's own code sample (the issue predates the code, so the code sample there was aspirational, not a implemented contract).
- A "no ADR covers this" claim must name the exact patterns and case-sensitivity searched; generic words such as "transport"/"strategy" match many unrelated ADRs (the earlier "no match" wording was false and failed verification). Search for the decision's specific symbols and phrases instead.
- `docs/messaging/toc.yml`, named as a "destination" in the pre-draft, no longer exists — but that is because of an unrelated, project-wide docs-platform migration (#916, just-the-docs theme) months later, not a regression of this issue's work. Before flagging a missing destination file as a gap, check `git log --follow --diff-filter=D` for it: a deliberate, broader removal is not the same as content silently dropped.
