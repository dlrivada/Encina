---
title: "What the NBomber broker scenarios measure (spike #2835)"
layout: default
nav_exclude: true
---

# What the NBomber broker scenarios measure (spike #2835)

This page answers [#2835](https://github.com/dlrivada/Encina/issues/2835): do the load tests published under the `brokers` family measure Encina's transport code? The audience is the maintainer, who has to choose what the family should measure, and the contributor who will change the scenarios. The evidence was read at `origin/main` `a14676f1d` on 2026-10-26. The recommendations are input; [#2835](https://github.com/dlrivada/Encina/issues/2835) belongs to the measurement-integrity EPIC [#2845](https://github.com/dlrivada/Encina/issues/2845).

## 1. Answer

**No.** Every measured step of the 14 broker scenarios calls a broker client library directly: RabbitMQ.Client, Confluent.Kafka, NATS.Net or MQTTnet. No Encina transport type is referenced or executed, so the published broker numbers describe the client libraries, not `Encina.RabbitMQ`, `Encina.Kafka`, `Encina.NATS` or `Encina.MQTT`.

## 2. Evidence

### 2.1 References

`tests/Encina.NBomber/Encina.NBomber.csproj` references the four client libraries (`:96-99`) and three of their Testcontainers modules (`:104-106`: RabbitMQ, Kafka and NATS; MQTT runs on the workflow's Mosquitto service). It has no `ProjectReference` to any of the four Encina transport packages, directly or through the Encina projects it does reference (core, Messaging, Tenancy, the database, caching and lock packages, `Encina.InMemory`). The only `using Encina.*` lines in `tests/Encina.NBomber/Scenarios/Brokers/` are the harness's own namespaces.

### 2.2 The measured calls

| Scenario | Measured call | Where |
| --- | --- | --- |
| `rabbitmq-publish-throughput` | `_publishChannel.BasicPublishAsync(...)` | `RabbitMQScenarioFactory.cs:57`, call at `:75` |
| `rabbitmq-consume-throughput` | `BasicPublishAsync` then `consumerChannel.BasicGetAsync(...)` | `:133`, `:151`, `:159` |
| `rabbitmq-concurrent-consumers` | `BasicPublishAsync` then `BasicGetAsync` | `:236`, `:254`, `:267` |
| `rabbitmq-publisher-confirms` | `BasicPublishAsync` | `:356`, `:376` |
| `kafka-produce-throughput` | `_producer.ProduceAsync(...)` | `KafkaScenarioFactory.cs:57`, `:71` |
| `kafka-batch-produce-throughput` | `ProduceAsync` and `Flush` | `:139`, `:166`, `:177` |
| `kafka-consume-throughput` | `ProduceAsync` then `consumer.Consume(...)` | `:243`, `:258`, `:265` |
| `kafka-partition-distribution` | `ProduceAsync` | `:335`, `:349` |
| `nats-publish-throughput` | `connection.PublishAsync(...)` | `NATSScenarioFactory.cs:56`, `:70` |
| `nats-request-reply` | NATS request and a raw `SubscribeAsync` responder | `:108`, `:157` |
| `nats-jetstream-publish` | `jsContext.PublishAsync(...)` | `:216`, `:229` |
| Three MQTT scenarios, including `mqtt-subscribe-throughput` | `_publishClient.PublishAsync(...)`; the subscriber uses `SubscribeAsync` | `MQTTScenarioFactory.cs:56`, `:123`, `:198`; calls at `:75`, `:142`, `:217`, `:245` |

### 2.3 How the numbers reach the dashboard

`.github/workflows/load-tests.yml` runs the job `run-broker-load-tests` (`:581`) over a matrix of the four providers (`:590`), with `--scenario brokers --broker-provider <provider>` (`:661-665`), thresholds from `.github/ci/nbomber-brokers-thresholds.json` (`:671`), and publishes `broker-load-metrics-<provider>` (`:694`), which `publish-load-tests.yml` turns into the `load-tests` dashboard. `tests/Encina.NBomber/README.md` calls the project a "load testing harness for Encina features" (line 3) and lists Brokers as "Message broker pub/sub" (line 15); it never says the Encina transports are bypassed.

### 2.4 Two related findings

- **The `brokers` area is never assigned.** `ExtractArea` in `.github/scripts/perf-report.cs` maps names that start with `broker-` to "brokers" (`:1032`), but the scenarios are named `rabbitmq-*`, `kafka-*`, `nats-*` and `mqtt-*`. They fall to the first-segment rule (`:1034-1035`) and appear as areas named after each broker.
- **Several scenarios have no Encina counterpart.** Three of the four Encina packages are publish-only. Their publisher interfaces expose:
  - RabbitMQ: `PublishAsync`, `SendToQueueAsync` and `RequestAsync`.
  - Kafka: `ProduceAsync`, `ProduceBatchAsync` and `ProduceWithHeadersAsync`.
  - NATS: `PublishAsync`, `RequestAsync` and `JetStreamPublishAsync`.
  - MQTT: `PublishAsync`, and also `SubscribeAsync` and `SubscribePatternAsync` (`src/Encina.MQTT/IMQTTMessagePublisher.cs:21`, `:39`, `:55`).

  No package has a hosted consumer, and only MQTT exposes subscription methods. The consume scenarios `rabbitmq-consume-throughput`, `rabbitmq-concurrent-consumers` and `kafka-consume-throughput`, and `kafka-partition-distribution`, measure broker behaviour that Encina does not wrap. `mqtt-subscribe-throughput` does have an Encina counterpart (`SubscribeAsync`) and can be paired.

### 2.5 For comparison

The other NBomber families do run Encina code: database (ADO.NET, Dapper, EF Core, MongoDB), caching (`Encina.Caching.*`), locking (`Encina.DistributedLock.*`), messaging (`Encina.InMemory`) and ID generation. No project in `tests/Encina.LoadTests` or `tests/Encina.BenchmarkTests` references the four transport packages; the only Kafka hits there are the Debezium CDC position benchmarks. No benchmark or load justification file exists for these four transports. Benchmarks for the transports are tracked by [#2831](https://github.com/dlrivada/Encina/issues/2831).

## 3. Settled points

| Point | Settled by |
| --- | --- |
| Publishing raw client throughput under the Encina load-test dashboard, unlabelled, is not acceptable: either the scenarios call Encina or the dashboard says they do not | #2835 body ("rewrite them ... or relabel the dashboard module honestly"); [AGENTS.md](../../AGENTS.md) sections 5 (transports stay coherent) and 9; EPIC #2845 ("missing data reads as fine" is the defect class) |
| Whatever the family measures, the four transports are treated alike | AGENTS.md section 5, transports row |
| The `ExtractArea` mismatch is a defect to fix whatever is decided | The workflow, the README and the threshold file all call the family `brokers` |
| Scenarios that call an Encina publisher assert its `Either` result: a `Left` counts as a failed request, never as success | AGENTS.md section 3 (errors are never swallowed) |
| AzureServiceBus and AmazonSQS stay with [#939](https://github.com/dlrivada/Encina/issues/939); the same-workload defect stays with [#1760](https://github.com/dlrivada/Encina/issues/1760) | #2835 out of scope |

## 4. Decisions

The issue said "no open design choices", but its own table offers two compliant outcomes (rewrite or relabel), and the evidence adds a third. The maintainer decided both dilemmas on 2026-10-26 (comment on [#2835](https://github.com/dlrivada/Encina/issues/2835)):

- **N1 = N1c, paired scenarios.** Each publish-type broker scenario runs twice under the same conditions, once through the Encina transport package and once through the client directly; the dashboard shows both and the difference (Encina's overhead).
- **N2 = N2b, consume and subscribe scenarios kept, labelled.** They stay as "client reference" rows with a justification, visibly separate from the paired rows, until an Encina consumer exists; then they are paired too.

Correction found in review after the decision: `Encina.MQTT` already exposes `SubscribeAsync` (section 2.4), so under N2b's own rule ("then they are paired too") `mqtt-subscribe-throughput` can be paired now; the other consume scenarios stay as client reference. This is flagged to the maintainer and noted on [#2908](https://github.com/dlrivada/Encina/issues/2908).

The options as they were presented are kept below for the record.

### N1. What the `brokers` family measures

| | N1a: rewrite to Encina publishers | N1b: relabel as broker-client reference | N1c: paired scenarios |
| --- | --- | --- | --- |
| What it is | Each publish-type scenario builds a `ServiceCollection` with `AddEncina<X>`, resolves `I<X>MessagePublisher` and measures `PublishAsync` (or `ProduceAsync`, `JetStreamPublishAsync`, `RequestAsync`) | Keep the scenarios; rename the family "broker client reference", say so in the README and the dashboard, and add load justification files for the four Encina transports | For every publish-type operation, an Encina scenario and the raw-client scenario run side by side, so the dashboard shows Encina's overhead over the client |
| Better when | Only Encina code should appear on an Encina dashboard | Load testing the thin publisher wrappers is judged not worth it before 1.0 | Users and maintainers want to know what Encina's serialization, logging and `Either` mapping cost on top of the client |
| Pros | Honest and simple | Least work | The only option that measures Encina and explains the number (overhead, not raw broker speed) |
| Cons | Loses the client baseline, so a regression cannot be split between Encina and the client | The four transports have no load measurement at all; justification files for thin wrappers are allowed for load tests (AGENTS.md section 9) but the EPIC asks for coverage, not justification, where code exists | Twice the scenarios; longer CI job |
| Effort | Medium (four sub-issues) | Small | Medium to large |

**Recommendation: N1c**, because a load number for a thin wrapper is only meaningful next to the number it wraps.

### N2. Consume and subscribe scenarios with no Encina API

| | N2a: remove them | N2b: keep them, labelled as broker-client reference | N2c: keep them until Encina has a consumer API |
| --- | --- | --- | --- |
| Better when | The dashboard should show only Encina code | N1c is chosen and the reference rows are useful context | A consumer API is planned soon |
| Cons | Loses context on broker capacity | Mixed family | Leaves unlabelled raw numbers in place meanwhile, which the settled point above rules out unless they are labelled |

N2c without a label is excluded by the first settled point. **Recommendation: N2b if N1c, N2a if N1a.**

## 5. Sub-issues

After the decisions the five drafts were opened as sub-issues of [#2845](https://github.com/dlrivada/Encina/issues/2845): [#2905](https://github.com/dlrivada/Encina/issues/2905) (RabbitMQ), [#2906](https://github.com/dlrivada/Encina/issues/2906) (Kafka), [#2907](https://github.com/dlrivada/Encina/issues/2907) (NATS) and [#2908](https://github.com/dlrivada/Encina/issues/2908) (MQTT), chained with blocked-by in that order because they share files, and [#2851](https://github.com/dlrivada/Encina/issues/2851) (area and README). Each is sized to one worker PR (one provider, at most about 15 files). For the four [TEST] issues, the first acceptance step records the baseline: the current numbers of that provider's raw scenarios from the last published `broker-load-metrics-<provider>` run.

| Issue | Title | Files (exact) | Acceptance |
| --- | --- | --- | --- |
| [#2905](https://github.com/dlrivada/Encina/issues/2905) | `[TEST]` RabbitMQ load scenarios measure Encina.RabbitMQ paired with RabbitMQ.Client | `tests/Encina.NBomber/Encina.NBomber.csproj`, `tests/Encina.NBomber/Scenarios/Brokers/RabbitMQScenarioFactory.cs`, `tests/Encina.NBomber/Scenarios/Brokers/Providers/RabbitMQProviderFactory.cs`, `.github/ci/nbomber-brokers-thresholds.json`, `tests/Encina.NBomber/README.md` | `rabbitmq-publish-throughput` and `rabbitmq-publisher-confirms` measure `IRabbitMQMessagePublisher.PublishAsync`; a `Left` counts as failure; each publish row is paired with its raw-client row; the consume rows stay as labelled client reference; the workflow run for `rabbitmq` passes its thresholds |
| [#2906](https://github.com/dlrivada/Encina/issues/2906) | `[TEST]` Kafka load scenarios measure Encina.Kafka paired with Confluent.Kafka | `Encina.NBomber.csproj`, `KafkaScenarioFactory.cs`, `Providers/KafkaProviderFactory.cs`, `nbomber-brokers-thresholds.json`, `README.md` | `kafka-produce-throughput` measures `ProduceAsync` and `kafka-batch-produce-throughput` measures `ProduceBatchAsync`; same rules |
| [#2907](https://github.com/dlrivada/Encina/issues/2907) | `[TEST]` NATS load scenarios measure Encina.NATS paired with NATS.Net | `Encina.NBomber.csproj`, `NATSScenarioFactory.cs`, `Providers/NATSProviderFactory.cs`, `nbomber-brokers-thresholds.json`, `README.md` | `nats-publish-throughput`, `nats-request-reply` (`RequestAsync`) and `nats-jetstream-publish` (`JetStreamPublishAsync`) measure the Encina publisher; same rules |
| [#2908](https://github.com/dlrivada/Encina/issues/2908) | `[TEST]` MQTT load scenarios measure Encina.MQTT paired with MQTTnet | `Encina.NBomber.csproj`, `MQTTScenarioFactory.cs`, `Providers/MQTTProviderFactory.cs`, `nbomber-brokers-thresholds.json`, `README.md` | The publish scenarios measure `IMQTTMessagePublisher.PublishAsync`, and `mqtt-subscribe-throughput` can be paired with `SubscribeAsync` (Encina has a subscription API for MQTT); same rules |
| [#2851](https://github.com/dlrivada/Encina/issues/2851) | `[BUG]` Broker load scenarios are not grouped under the `brokers` area, and the README does not say what they measure | `.github/scripts/perf-report.cs`, `tests/Encina.NBomber/README.md` | `ExtractArea` maps `rabbitmq-`, `kafka-`, `nats-` and `mqtt-` names to "brokers" (or the names gain a `broker-` prefix); the README states per scenario whether it measures Encina code or the client, and drops `rabbitmq-batch-publish` (listed at `README.md:250`, no such scenario exists); a unit check of `ExtractArea` covers the four prefixes |

Drafts 1-4 share `Encina.NBomber.csproj`, `nbomber-brokers-thresholds.json` and the README, so they run one at a time, not in parallel.

## 6. Unknowns

- How the published dashboard labels these rows today, given the `ExtractArea` mismatch (the published data was not opened).

## Related issues

[#2835](https://github.com/dlrivada/Encina/issues/2835), [#2845](https://github.com/dlrivada/Encina/issues/2845), [#1760](https://github.com/dlrivada/Encina/issues/1760), [#939](https://github.com/dlrivada/Encina/issues/939), [#2831](https://github.com/dlrivada/Encina/issues/2831), [#2851](https://github.com/dlrivada/Encina/issues/2851), [#2905](https://github.com/dlrivada/Encina/issues/2905), [#2906](https://github.com/dlrivada/Encina/issues/2906), [#2907](https://github.com/dlrivada/Encina/issues/2907), [#2908](https://github.com/dlrivada/Encina/issues/2908).
