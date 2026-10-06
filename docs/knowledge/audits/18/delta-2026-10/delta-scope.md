# Delta scope of issue #18 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published stage files of the original audit (docs/knowledge/audits/18/stages/).

## Knowledge record (docs/knowledge/issues/18.md)

```yaml
schema: 1
nav_exclude: true
issue: 18
title: "[SPIKE] Implement Strategy pattern for Messaging Transports (RabbitMQ, Kafka, NATS, etc.)"
closed: 2025-12-23
state_reason: completed
outcome: rejected-reasoned
type: spike
area: messaging
review: verified
packages:
  - "Encina.RabbitMQ"
  - "Encina.Kafka"
  - "Encina.NATS"
  - "Encina.AzureServiceBus"
  - "Encina.AmazonSQS"
  - "Encina.Redis.PubSub"
  - "Encina.InMemory"
  - "Encina.MQTT"
  - "Encina.gRPC"
  - "Encina.GraphQL"
prs:
linked_prs:
knowledge:
  - kind: rejected-alternative
    statement: "Do not implement a Strategy + Orchestrator + Provider pattern (IMessageBrokerStrategy / IEventStreamingStrategy / IPubSubStrategy, or a single IMessageTransport abstraction) for messaging transports; each transport (RabbitMQ, Kafka, NATS, ...) keeps its own independent package exposing its full native API, because unifying the APIs would lose transport-specific capabilities (offsets, exchanges, subjects) that are the reason users pick a given transport."
    current: yes
    sources:
      - "quote: \"After careful analysis, we decided NOT to implement the Strategy pattern for messaging transports [...] Each transport has fundamentally different APIs [...] Unifying APIs would lose transport-specific features.\" (#18, 2025-12-23)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/transports.md"
      - kind: rule
        status: done
        target: "AGENTS.md"
  - kind: decision
    statement: "NATS ships as a single package (Encina.NATS) with two explicit modes selected through configuration: pub/sub (Core) and streaming (JetStream), rather than being split into two packages or forced into one of the three strategy categories."
    current: yes
    sources:
      - "quote: \"NATS multi-strategy? One package, two modes: UseNatsCore() for pub/sub, UseNatsJetStream() for streaming\" (#18, 2025-12-23)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/transports.md"
      - kind: rule
        status: planned
        target: "src/Encina.NATS/ServiceCollectionExtensions.cs"
  - kind: decision
    statement: "No runtime strategy switching for testing; Encina.InMemory is the recommended and sufficient substitute for every transport in tests."
    current: yes
    sources:
      - "quote: \"Runtime switching for testing? NO - Use Encina.InMemory for tests (purpose-built for this)\" (#18, 2025-12-23)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/transports.md"
  - kind: decision
    statement: "Transport choice is per-application (architectural), not per-module or per-message-type."
    current: yes
    sources:
      - "quote: \"Per-module vs per-message strategy? Per-application - transport choice is architectural, not per-message\" (#18, 2025-12-23)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/transports.md"
  - kind: decision
    statement: "gRPC and GraphQL are excluded from the messaging-transport categorization; they are classified separately as \"API Bridge\" (request/response, RPC), not messaging transports."
    current: yes
    sources:
      - "quote: \"gRPC/GraphQL as RPC Strategy? Keep outside pattern - they're API bridges, not messaging transports\" (#18, 2025-12-23)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/transports.md"
  - kind: rejected-alternative
    statement: "Option A (three strategy interfaces: IMessageBrokerStrategy, IEventStreamingStrategy, IPubSubStrategy, each with a strategy-specific orchestrator) was rejected as too complex, and because NATS spans more than one category."
    current: yes
    sources:
      - "paraphrase: issue body, \"Option A: Three Strategy Types\", Cons: \"More complex, NATS spans multiple strategies\" (#18, 2025-12-23)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/transports.md"
  - kind: rejected-alternative
    statement: "Option B (a single IMessageTransport abstraction with capability flags) was rejected because it does not capture the fundamental semantic differences between transports and is easy to misuse."
    current: yes
    sources:
      - "paraphrase: issue body, \"Option B: Single Transport Abstraction\", Cons: \"Doesn't capture fundamental semantic differences, easy to misuse\" (#18, 2025-12-23)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/transports.md"
  - kind: rejected-alternative
    statement: "Option C (two categories: persistent vs. ephemeral transports) was rejected because it loses the distinction between queue-based (broker) and log-based (streaming) systems."
    current: yes
    sources:
      - "paraphrase: issue body, \"Option C: Two Categories (Persistent vs Ephemeral)\", Cons: \"Loses distinction between queue-based and log-based\" (#18, 2025-12-23)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/transports.md"
  - kind: gotcha
    statement: "docs/messaging/transports.md documents the NATS configuration API as two distinct extension methods, services.AddEncinaNatsCore(...) and services.AddEncinaNatsJetStream(...), but Encina.NATS's ServiceCollectionExtensions.cs exposes only a single AddEncinaNATS(Action<EncinaNATSOptions> configure) method with a UseJetStream boolean option; the documented method names do not exist in the package."
    current: yes
    sources:
      - "paraphrase: verified by reading src/Encina.NATS/ServiceCollectionExtensions.cs against docs/messaging/transports.md lines 293 and 305 (#18, 2026-09-27)"
    destinations:
      - kind: docs
        status: planned
        target: "docs/messaging/transports.md"
audit:
  checklist: 1
  date: 2026-09-27
  verdict: not-audited
  record: "docs/knowledge/audits/issue-18.md"
remediation:
  - "docs/messaging/transports.md's NATS section documents AddEncinaNatsCore/AddEncinaNatsJetStream extension methods that do not exist in src/Encina.NATS; the package only has AddEncinaNATS with a UseJetStream option. Fix the doc to match the shipped API (or add the two methods, whichever is intended), else developers following the guide get a compile error."
```

## From the original archivist.md (docs/knowledge/audits/18/stages/archivist.md)

Issue #18 is a SPIKE that ended in a "no-go" decision (no Strategy pattern for messaging transports) plus a documentation deliverable. There is no closing PR with code changes; the only referenced commit is `6a72163a1a61c0f52a1d8e2e38a49fd6931af5a9` ("docs: add comprehensive Messaging Transports documentation"), which touched only:

- `docs/messaging/transports.md` — created, still present today (569 lines at commit time; 14,986 bytes today), still has the decision flowchart (4 mermaid diagrams), transport comparison table, per-transport guides and FAQ.
- `docs/messaging/index.md` — updated with a "Messaging Transports" section and a link to `transports.md`; still present with both today.
- `docs/messaging/toc.yml` — updated at the time, but this file no longer exists anywhere under `docs/`: it was removed in commit `5d8b6d3203a2139a01e84c9b15215214c01b2535` ("docs: migrate to just-the-docs theme with proper navigation (#916)", 2026-03-27), a broader docs-platform migration unrelated to #18. Its role is now served by front matter (`layout: default`, `parent: "Messaging"`) in each page, verified present in `transports.md`. Nothing further to scope for `toc.yml`: it was a deliberate, project-wide removal, not a regression of #18's work.

No source code (`src/`) was touched by this issue. The 10 packages the issue named or that exist today for messaging transports (`Encina.RabbitMQ`, `Encina.Kafka`, `Encina.NATS`, `Encina.AzureServiceBus`, `Encina.AmazonSQS`, `Encina.Redis.PubSub`, `Encina.InMemory`, `Encina.MQTT`, `Encina.gRPC`, `Encina.GraphQL`) all still exist under `src/` today, each as an independent package — consistent with the decision (verified with `Get-ChildItem src`). The issue explicitly scoped out "Full implementation of all strategy-specific orchestrators" and "gRPC/GraphQL categorization" — both remain out of scope by design, not by omission.

## From the original code.md (docs/knowledge/audits/18/stages/code.md)

Per `artifacts/knowledge/stages/archivist.md`, issue #18 is a documentation-only SPIKE ("no Strategy pattern for messaging transports") whose only artifact is commit `6a72163a` touching `docs/messaging/transports.md` and `docs/messaging/index.md`. No `src/` file was part of the issue's own diff.

Scope correction: because the issue's outcome is a standing decision that each of the 10 transport packages (`Encina.RabbitMQ`, `Encina.Kafka`, `Encina.NATS`, `Encina.AzureServiceBus`, `Encina.AmazonSQS`, `Encina.Redis.PubSub`, `Encina.InMemory`, `Encina.MQTT`, `Encina.gRPC`, `Encina.GraphQL`) must stay independent yet coherent (AGENTS.md §5, "Transports" row: every provider MUST support Send/Publish, subscription management, error handling and DLQ, metadata propagation), the code stage walked out to these implementations (as the #16 lesson directs for abstraction/decision-only issues). Files actually read: `src/Encina.NATS/{ServiceCollectionExtensions.cs, NATSMessagePublisher.cs, INATSMessagePublisher.cs}`, `src/Encina.RabbitMQ/{ServiceCollectionExtensions.cs, RabbitMQMessagePublisher.cs, EncinaRabbitMQOptions.cs}`, `src/Encina.MQTT/{ServiceCollectionExtensions.cs, MQTTMessagePublisher.cs}`, `src/Encina.Kafka/{ServiceCollectionExtensions.cs, KafkaMessagePublisher.cs}`, `src/Encina.AzureServiceBus/{ServiceCollectionExtensions.cs, AzureServiceBusMessagePublisher.cs}`, `src/Encina.AmazonSQS/ServiceCollectionExtensions.cs`, `src/Encina.Redis.PubSub/{ServiceCollectionExtensions.cs, RedisPubSubMessagePublisher.cs}`, `src/Encina.InMemory/{ServiceCollectionExtensions.cs, InMemoryMessageBus.cs}`, and full directory listings of `Encina.RabbitMQ`, `Encina.Kafka`, `Encina.AzureServiceBus`, `Encina.AmazonSQS`, `Encina.NATS`, `Encina.InMemory`, plus `docs/messaging/transports.md`.


