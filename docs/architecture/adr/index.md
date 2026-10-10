---
title: ADRs
layout: default
parent: Architecture
nav_order: 10
has_children: true
---

# Architecture Decision Records

| ADR | Title |
|-----|-------|
| [001](001-railway-oriented-programming.md) | Railway Oriented Programming |
| [002](002-dependency-injection-strategy.md) | Dependency Injection Strategy |
| [003](003-caching-strategy.md) | Caching Strategy |
| [004](004-reject-mediator-result.md) | Reject Mediator Result |
| [005](005-reject-source-generators.md) | Reject Source Generators |
| [006](006-pure-rop-exception-handling.md) | Pure ROP Exception Handling |
| [007](007-extensibility-strategy-v2.md) | Extensibility Strategy v2 |
| [008](008-aspire-vs-testcontainers-testing-strategy.md) | Aspire vs Testcontainers |
| [009](009-remove-oracle-provider-pre-1.0.md) | Remove Oracle Provider Pre-1.0 |
| [010](010-database-sharding.md) | Database Sharding |
| [011](011-id-generation-multi-strategy.md) | ID Generation Multi-Strategy |
| [012](012-sharded-read-write-separation.md) | Sharded Read/Write Separation |
| [013](013-reference-tables.md) | Reference Tables |
| [014](014-data-residency-gdpr-chapter-v.md) | Data Residency (GDPR Chapter V) |
| [015](015-xacml-3.0-abac-foundation.md) | XACML 3.0 ABAC Foundation |
| [016](016-roslyn-expression-language.md) | Roslyn Expression Language |
| [017](017-eel-naming-design.md) | EEL Naming Design |
| [018](018-cross-cutting-integration-principle.md) | Cross-Cutting Integration Principle |
| [019](019-compliance-event-sourcing-marten.md) | Compliance Event Sourcing (Marten) |
| [020](020-temporal-crypto-shredding-audit-store.md) | Temporal Crypto-Shredding Audit Store |
| [021](021-eventid-uniqueness-enforcement.md) | EventId Uniqueness Enforcement |
| [022](022-provider-agnostic-attestation.md) | Provider-Agnostic Attestation |
| [023](023-coverage-strategy-codecov-sonarcloud.md) | Coverage Strategy (Codecov + SonarCloud) |
| [024](024-remove-sqlite-provider-pre-1.0.md) | Remove SQLite Provider Pre-1.0 |
| [025](025-performance-measurement-infrastructure.md) | Performance Measurement Infrastructure |
| [027](027-marten-as-the-event-sourcing-provider.md) | Marten Is the Event-Sourcing Provider; EventStoreDB Deprecated |
| [028](028-domain-events-versus-integration-events.md) | Domain Events vs Integration Events (Outbox only) |
| [029](029-recoverability-error-classification.md) | Recoverability Error Classification |
| [030](030-encryption-at-the-serializer-level.md) | Encryption at the Serializer Level (AES-256-GCM) |
| [031](031-retention-erasure-port.md) | Retention Enforcement Erases Through Its Own Category-Scoped Port |
| [034](034-crypto-shredding-through-the-stj-contract.md) | Crypto-Shredding Runs Through the System.Text.Json Contract |
| [036](036-three-audit-stores.md) | Three Purpose-Named Audit Stores: Operation, Entity Change and Read Access |
| [046](046-persistent-dead-letter-queue.md) | Persistent Dead Letter Queue: Oldest-First Contract, Idempotent Capture, Tenant Column |
| [048](048-inbox-record-vs-business-transaction.md) | The Inbox Record and the Business Transaction: Enlisted and Independent Writes |

## Reserved numbers

An ADR takes the next number that is neither used nor reserved. A plan or spike that needs an ADR before the ADR is written reserves the next free number in this table, in the same PR. A reservation is removed when the ADR is merged (its row moves to the table above) or when the plan is dropped.

| Number | Reserved by | Topic | Status |
| ------ | ----------- | ----- | ------ |
| 026 | [otlp-exporter-implementation-plan-1043.md](../../plans/otlp-exporter-implementation-plan-1043.md) (#1043) | OTLP exporter opt-in decision | Reserved, ADR not written |
| 032 | [retention-floor-implementation-plan-1187.md](../../plans/retention-floor-implementation-plan-1187.md) (#1187) | Retention floor and anchored periods | Reserved, ADR not written |
| 033 | [blocked-data-state-implementation-plan-1189.md](../../plans/blocked-data-state-implementation-plan-1189.md) (#1189) | Composable row filters and blocked data state | Reserved, ADR not written |
| 035 | [security-context-population-implementation-plan-1705.md](../../plans/security-context-population-implementation-plan-1705.md) (#1705) | One request identity model | Reserved, ADR not written |
| 037 | [outbox-atomicity-ado-implementation-plan-718.md](../../plans/outbox-atomicity-ado-implementation-plan-718.md) (#718) | Transactional outbox | Reserved, ADR not written |
| 038 | [pre-release-checklist-implementation-plan-104.md](../../plans/pre-release-checklist-implementation-plan-104.md) (#104) | Release readiness gate | Reserved, ADR not written |
| 039 | [break-the-glass-implementation-plan-1244.md](../../plans/break-the-glass-implementation-plan-1244.md) (#1244) | Break-the-glass emergency access | Reserved, ADR not written |
| 040 | [enisa-single-entry-point-implementation-plan-812.md](../../plans/enisa-single-entry-point-implementation-plan-812.md) (#812) | Incident reporting adapters on the breach stream | Reserved, ADR not written |
| 041 | [aiact-record-keeping-implementation-plan-842.md](../../plans/aiact-record-keeping-implementation-plan-842.md) (#842) | AI Act record-keeping log | Reserved, ADR not written |
| 042 | [data-subject-representation-implementation-plan-1197.md](../../plans/data-subject-representation-implementation-plan-1197.md) (#1197) | Data-subject representation shape | Reserved, ADR not written |
| 043 | [processor-row-claiming-implementation-plan-1251.md](../../plans/processor-row-claiming-implementation-plan-1251.md) (#1251) | Processor row claiming | Reserved, ADR not written |
| 044 | [aiact-marten-event-sourcing-implementation-plan-847.md](../../plans/aiact-marten-event-sourcing-implementation-plan-847.md) (#847) | Deterministic natural-key stream identity for compliance aggregates | Reserved, ADR not written |
| 045 | [special-category-conditions-implementation-plan-1196.md](../../plans/special-category-conditions-implementation-plan-1196.md) (#1196) | Legal grounds: Art. 6 basis and Art. 9(2) condition as one model | Reserved, ADR not written |
| 047 | [abac-decision-audit-implementation-plan-751.md](../../plans/abac-decision-audit-implementation-plan-751.md) (#751) | ABAC decision audit trail | Reserved, ADR not written |
| 049 | [pipeline-execution-order-implementation-plan-2184.md](../../plans/pipeline-execution-order-implementation-plan-2184.md) (#2184) | Pipeline behavior execution-order contract (named stages) | Reserved, ADR not written |
