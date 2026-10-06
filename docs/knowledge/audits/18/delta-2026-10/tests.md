## Coverage measured

Delta `rules-2026-10`, rule (b) only. Scope: the ten messaging transports of the delta scope (`Encina.RabbitMQ`, `Encina.Kafka`, `Encina.NATS`, `Encina.AzureServiceBus`, `Encina.AmazonSQS`, `Encina.Redis.PubSub`, `Encina.InMemory`, `Encina.MQTT`, `Encina.gRPC`, `Encina.GraphQL`). The worktree is at `origin/main` (`3b4e49fe`) plus the audit's own commits. Every figure below was measured in this stage, none carried forward.

Method (build configuration **Release**, as CI):

- Per flag: `dotnet build <wt>\tests\Encina.<Flag>Tests\Encina.<Flag>Tests.csproj -c Release`, then `dotnet test <same project> -c Release --no-build --filter "<filter>" --collect "XPlat Code Coverage" --results-directory <wt>\artifacts\audit\coverage\<flag>` with `<flag>` = `unit`, `guard`, `contract`, `property`, `integration`.
- Filters (bare package namespaces; the tests of Redis.PubSub live under `Messaging.RedisPubSub`): unit `FullyQualifiedName~Encina.UnitTests.<ns>` for `RabbitMQ`, `Kafka`, `NATS`, `AzureServiceBus`, `AmazonSQS`, `Messaging.RedisPubSub`, `InMemory`, `MQTT`, `gRPC`, `GraphQL`, plus `OptionsSecretProtectionTests` and `LateConfigureEndpointValidationTests` (they execute option and registration code of these packages); guard the same list under `Encina.GuardTests.<ns>`; contract `Encina.ContractTests.Messaging.RedisPubSub`; property `Encina.PropertyTests.Messaging.RedisPubSub`; integration `Encina.IntegrationTests.MessageBrokers`.
- Results: unit 630 passed, guard 129 passed, contract 16 passed, property 6 passed, integration 13 passed (8 s), 0 failed, 0 skipped.
- Coverable lines are counted as the unique line numbers per source file in the Cobertura report (maximum hits across the class entries of that file, generated `obj\` files excluded). Contract, property and integration reports match files with `src\<Package>\...` or `<Package>\...` alike.
- Integration infrastructure: Docker 29.8.1 was running. The four broker fixtures that have integration tests (RabbitMQ, Kafka, NATS, MQTT) started and their 13 tests passed; no container failed to start. Redis, Amazon SQS (LocalStack), Azure Service Bus, gRPC, GraphQL and InMemory have no integration test, so no container was attempted for them.
- "Package vs target" compares the aggregate of the manifest-applicable files of the flag (those whose `defaultTests` include it) with the package `targets` value. The manifests hold no per-file target, so per-file rows have nothing to pass or fail against (that is rule (b) finding material).
- Contract and property: tests of these flags exist only for Redis.PubSub (name search over `tests\Encina.ContractTests` and `tests\Encina.PropertyTests` for the ten public interfaces and package namespaces: one hit, `Messaging\RedisPubSub\RedisPubSubPublisherContractTests.cs`). For the other nine packages both flags are "not measured: no test exists".
- Load and benchmark are not coverage flags and were not run (see Informational).

Per file: measured unit | guard | integration (percent, covered/coverable); `[...]` is the manifest `defaultTests`. Files without a coverable line (`Log.cs`, `GlobalSuppressions.cs`, and the interface files that declare only interfaces) are not listed; their empty `defaultTests` with a `reason` is correct.

**Encina.RabbitMQ** (package unit 92.03% vs 55 pass; guard 30.71% (39/127) vs 15 pass; integration: no package target)

| File | Unit | Guard | Integration |
| --- | --- | --- | --- |
| `EncinaRabbitMQOptions.cs` [unit] | 100.00 (11/11) | 90.91 (10/11) | 0.00 (0/11) |
| `Health/RabbitMQHealthCheck.cs` [unit, guard] | 100.00 (10/10) | 30.00 (3/10) | 70.00 (7/10; missed 41, 46, 48) |
| `RabbitMQMessagePublisher.cs` [unit, guard] | 100.00 (79/79) | 22.78 (18/79) | 0.00 (0/79) |
| `ServiceCollectionExtensions.cs` [unit, guard] | 71.05 (27/38; missed 44-51, 53, 58-59) | 47.37 (18/38) | 0.00 (0/38) |

**Encina.Kafka** (package unit 91.25% (146/160) vs 55 pass; guard 22.00% (33/150) vs 15 pass)

| File | Unit | Guard | Integration |
| --- | --- | --- | --- |
| `EncinaKafkaOptions.cs` [unit] | 100.00 (10/10) | 100.00 (10/10) | 0.00 (0/10) |
| `Health/KafkaHealthCheck.cs` [unit, guard] | 100.00 (9/9) | 33.33 (3/9) | 77.78 (7/9; missed 45, 47) |
| `IKafkaMessagePublisher.cs` [] (declares the record `KafkaDeliveryResult`, lines 66-70) | 100.00 (5/5) | 100.00 (5/5) | 0.00 (0/5) |
| `KafkaMessagePublisher.cs` [unit, guard] | 100.00 (103/103) | 14.56 (15/103) | 0.00 (0/103) |
| `ServiceCollectionExtensions.cs` [unit, guard] | 63.16 (24/38; missed 44-56, 58) | 39.47 (15/38) | 0.00 (0/38) |

**Encina.NATS** (package unit 98.78% (162/164) vs 55 pass; guard 30.53% (40/131) vs 15 pass)

| File | Unit | Guard | Integration |
| --- | --- | --- | --- |
| `EncinaNATSOptions.cs` [unit] | 100.00 (10/10) | 100.00 (10/10) | 0.00 (0/10) |
| `EncinaNATSOptionsValidator.cs` [unit] | 100.00 (23/23) | 95.65 (22/23) | 0.00 (0/23) |
| `Health/NATSHealthCheck.cs` [unit, guard] | 100.00 (10/10) | 30.00 (3/10) | 70.00 (7/10; missed 44, 46, 48) |
| `INATSMessagePublisher.cs` [] (declares the record `NATSPublishAck`, lines 63-66) | 100.00 (4/4) | 100.00 (4/4) | 0.00 (0/4) |
| `NATSMessagePublisher.cs` [unit, guard] | 100.00 (89/89) | 17.98 (16/89) | 0.00 (0/89) |
| `ServiceCollectionExtensions.cs` [unit, guard] | 93.75 (30/32; missed 56-57) | 65.62 (21/32) | 0.00 (0/32) |

**Encina.AzureServiceBus** (package unit 95.74% (135/141) vs 55 pass; guard 32.82% (43/131) vs 15 pass; integration: not measured, no test)

| File | Unit | Guard | Integration |
| --- | --- | --- | --- |
| `EncinaAzureServiceBusOptions.cs` [unit] | 100.00 (10/10) | 90.00 (9/10) | not measured: no test |
| `Health/AzureServiceBusHealthCheck.cs` [unit, guard] | 100.00 (10/10) | 30.00 (3/10) | not measured: no test |
| `AzureServiceBusMessagePublisher.cs` [unit, guard] | 93.81 (91/97; missed 69, 111, 157, 188, 204-205) | 16.49 (16/97) | not measured: no test |
| `ServiceCollectionExtensions.cs` [unit, guard] | 100.00 (24/24) | 100.00 (24/24) | not measured: no test |

**Encina.AmazonSQS** (package unit 95.69% (222/232) vs 55 pass; guard 21.39% (43/201) vs 15 pass; integration: not measured, no test)

| File | Unit | Guard | Integration |
| --- | --- | --- | --- |
| `EncinaAmazonSQSOptions.cs` [unit] | 100.00 (11/11) | 100.00 (11/11) | not measured: no test |
| `EncinaAmazonSQSOptionsValidator.cs` [unit] | 100.00 (20/20) | 90.00 (18/20) | not measured: no test |
| `Health/AmazonSQSHealthCheck.cs` [unit, guard] | 100.00 (14/14) | 21.43 (3/14) | not measured: no test |
| `AmazonSQSMessagePublisher.cs` [unit, guard] | 100.00 (155/155) | 12.26 (19/155) | not measured: no test |
| `ServiceCollectionExtensions.cs` [unit, guard] | 68.75 (22/32; missed 52-56, 61-65) | 65.62 (21/32) | not measured: no test |

**Encina.Redis.PubSub** (package unit 75.25% (76/101) vs 55 pass; guard 19.35% (18/93) vs 15 pass; contract and property: no package target)

| File | Unit | Guard | Contract | Property | Integration |
| --- | --- | --- | --- | --- | --- |
| `EncinaRedisPubSubOptions.cs` [unit] | 100.00 (8/8) | 75.00 (6/8) | 75.00 (6/8) | 87.50 (7/8; missed 37) | not measured: no test |
| `RedisPubSubMessagePublisher.cs` [unit, guard] | 70.42 (50/71; missed 95, 97, 99-100, 102-103, 105-106, 118, 120, 122-123, 125-126, 128-129, 172, 174-175, 179-180) | 23.94 (17/71) | 23.94 (17/71) | 0.00 (0/71) | not measured: no test |
| `ServiceCollectionExtensions.cs` [unit, guard] | 81.82 (18/22; missed 40-43) | 4.55 (1/22) | 0.00 (0/22) | 0.00 (0/22) | not measured: no test |

**Encina.InMemory** (package unit under the current manifest 22.22% (4/18) vs 55 **fail**, because `InMemoryMessageBus.cs` is classified as an interface; guard: no data, 0/13 on the one guard-applicable file vs 15 **fail** (not measured: no guard test); with the bus classified correctly unit would be 74.22% (95/128))

| File | Unit | Guard | Integration |
| --- | --- | --- | --- |
| `EncinaInMemoryOptions.cs` [unit] | 80.00 (4/5; missed 21) | not measured: no guard test, the package is absent from the guard report | not applicable (in-process) |
| `InMemoryMessageBus.cs` [] (manifest says "Interface — no implementation") | 82.73 (91/110; missed 49-55, 57-63, 76, 174, 193, 195-196) | not measured: no guard test | not applicable (in-process) |
| `ServiceCollectionExtensions.cs` [unit, guard] | 0.00 (0/13) | not measured: no guard test | not applicable (in-process) |

**Encina.MQTT** (package unit 72.90% (156/214) vs 55 pass; guard 21.78% (44/202) vs 15 pass)

| File | Unit | Guard | Integration |
| --- | --- | --- | --- |
| `EncinaMQTTOptions.cs` [unit] | 100.00 (12/12) | 91.67 (11/12) | 0.00 (0/12) |
| `Health/MQTTHealthCheck.cs` [unit, guard] | 100.00 (7/7) | 42.86 (3/7) | 100.00 (7/7) |
| `MQTTMessagePublisher.cs` [unit, guard] | 69.86 (102/146; missed 108, 110-116, 118, 120-122, 124, 126, 128-129, 142, 144-150, 152, 154-156, 158, 160, 162-163, 220, 222-224, 226-227, 307, 309-311, 313-314) | 10.96 (16/146) | 0.00 (0/146) |
| `ServiceCollectionExtensions.cs` [unit, guard] | 71.43 (35/49; missed 45-46, 48-52, 54, 56, 59, 61, 64, 67, 69) | 51.02 (25/49) | 0.00 (0/49) |

**Encina.gRPC** (package unit 91.58% (185/202) vs 55 pass; guard 23.59% (46/195) vs 15 pass; integration: not measured, no test)

| File | Unit | Guard | Integration |
| --- | --- | --- | --- |
| `CachingTypeResolver.cs` [unit, guard] | 100.00 (7/7) | 100.00 (7/7) | not measured: no test |
| `EncinaGrpcOptions.cs` [unit] | 100.00 (7/7) | 100.00 (7/7) | not measured: no test |
| `GrpcMediatorService.cs` [unit, guard] (declares `GrpcEncinaService` and `GrpcSerializationException`) | 89.51 (145/162; missed 178-181, 214-217, 319-323, 345-348) | 12.35 (20/162) | not measured: no test |
| `Health/GrpcHealthCheck.cs` [unit, guard] | 100.00 (10/10) | 30.00 (3/10) | not measured: no test |
| `ServiceCollectionExtensions.cs` [unit, guard] | 100.00 (16/16) | 100.00 (16/16) | not measured: no test |

**Encina.GraphQL** (package unit 84.88% (146/172) vs 55 pass; guard 26.19% (22/84) vs 15 pass; integration: not measured, no test)

| File | Unit | Guard | Integration |
| --- | --- | --- | --- |
| `EncinaGraphQLOptions.cs` [unit] | 100.00 (8/8) | 100.00 (8/8) | not measured: no test |
| `GraphQLMediatorBridge.cs` [unit, guard] (declares `GraphQLEncinaBridge`) | 85.29 (58/68; missed 63, 65-68, 105, 107-110) | 20.59 (14/68) | not measured: no test |
| `Pagination/Connection.cs` [unit] | 100.00 (27/27) | 14.81 (4/27) | not measured: no test |
| `Pagination/ConnectionExtensions.cs` [unit] | 100.00 (40/40) | 32.50 (13/40) | not measured: no test |
| `Pagination/Edge.cs` [unit] | 100.00 (2/2) | 100.00 (2/2) | not measured: no test |
| `Pagination/RelayPageInfo.cs` [unit] | 100.00 (11/11) | 36.36 (4/11) | not measured: no test |
| `ServiceCollectionExtensions.cs` [unit, guard] | 0.00 (0/16) | 50.00 (8/16; missed 28-35) | not measured: no test |

## Findings

1. **Major** — `.github/coverage-manifest/Encina.RabbitMQ.json` (57 lines; `targets` at :5 holds only `guard` 15 and `unit` 55): no file of `Encina.RabbitMQ` has per-file targets in the manifest (the only proposal for any of its files is the unit and guard item for `RabbitMQMessagePublisher.cs` in open #1847, amended in the table below); flags that apply: `EncinaRabbitMQOptions.cs` unit; `Health/RabbitMQHealthCheck.cs` unit, guard, integration; `RabbitMQMessagePublisher.cs` unit, guard, integration, contract; `ServiceCollectionExtensions.cs` unit, guard, integration. The package has no `integration` key although three integration tests run against `RabbitMqFixture` (`tests/Encina.IntegrationTests/MessageBrokers/RabbitMQ/RabbitMQHealthCheckIntegrationTests.cs:14`), and the package unit and guard values sit far below the measured 92.03% and 30.71%.
   - Proposed package `targets`: `unit` 90, `guard` 30, `integration` 60 (provisional, aggregate of the integration proposals below); add `integration` to `defaultTests` of the health check, publisher and registration files.
   - `EncinaRabbitMQOptions.cs`: unit 100 — all 11 property-initializer lines run whenever a test constructs the options and the file has no branch.
   - `Health/RabbitMQHealthCheck.cs`: unit 100 — both `IsOpen` branches and the `BrokerUnreachableException` catch (lines 39-48) run with a substituted provider (10/10). guard 30 — null-check guard tests reach only constructor lines 27, 29-30; the class validates no argument (3/10). integration 70 — the healthy path runs against the container (7/10); lines 41, 46, 48 (closed connection, unreachable broker) need a stopped broker and are covered at unit level.
   - `RabbitMQMessagePublisher.cs`: unit and guard are already proposed by open #1847 (item "Per-file coverage targets and justifications for nine transport and API bridge publishers"); this finding amends them (the orchestrator posts the table as a comment on #1847) and adds the flags #1847 does not cover (integration, contract).

   | Flag | #1847 value | Proposed value | Justification |
   | --- | --- | --- | --- |
   | unit | 90 | 100 | `IConnection` and `IChannel` are substituted (`tests/Encina.UnitTests/RabbitMQ/Publishing/RabbitMQMessagePublisherTests.cs:20-21`) and all 79 lines run (79/79); 90 would let 7 lines lose their tests without failing (72 of 79 suffice). |
   | guard | 20 | 22 | the guard-reachable lines are the constructor lines and the `ThrowIfNull` checks at 34-37, 52, 100, 101, 148 (18/79 = 22.78%, every check is covered); 20 tolerates losing two of them (16 of 79 suffice). |
   | integration | not in #1847 | 65 (derived, provisional) | success paths of each publish method against `RabbitMqFixture`; no publisher integration test exists (0/79) and failure branches stay at unit level. |
   | contract | not in #1847 | 0 (provisional) | no contract test of `IRabbitMQMessagePublisher` exists and `AGENTS.md` section 9 requires one for a public API; raise to the constructor-and-null-check level (22) when written. |
   - `ServiceCollectionExtensions.cs`: unit 76 (derived) — lines 58-59 run with an `IConnection` substitute registered first (no seam needed), while 44-51 and 53 only run when `IConnection` is resolved from the delegate, which needs a reachable broker or the seam of #1610 (this corrects #1628, which treats the whole group as seam-blocked); 29/38 = 76.3%. guard 47 — 18/38 (`ThrowIfNull` at 24 plus the registration lines the guard tests build). integration 90 (derived, provisional) — an integration test that builds the provider with `AddEncinaRabbitMQ` against `RabbitMqFixture` runs the blocking factories (44-59), the only way to execute them without the seam.
   - Open issues: #1628 (still accurate for this file: 27/38, missed 44-51, 53, 58-59), #1610 (sync-over-async), #1625 (scope lifetime); #1847 proposes the publisher's unit and guard targets (amended above); no open issue sets targets for the other files, the package keys, integration or contract (search of the bodies of all 963 open issues on 2026-10-06).

2. **Major** — `.github/coverage-manifest/Encina.Kafka.json` (57 lines; `targets` at :5 holds only `guard` 15 and `unit` 55): no file of `Encina.Kafka` has per-file targets in the manifest (the only proposal for any of its files is the unit and guard item for `KafkaMessagePublisher.cs` in open #1847, amended in the table below); flags that apply: options unit; `Health/KafkaHealthCheck.cs` unit, guard, integration; `IKafkaMessagePublisher.cs` unit (classification defect, see below); `KafkaMessagePublisher.cs` unit, guard, integration, contract; `ServiceCollectionExtensions.cs` unit, guard. No package `integration` key although `KafkaHealthCheckIntegrationTests` run against `KafkaFixture`; package unit and guard values sit far below the measured 91.25% and 22.00%.
   - Classification defect (Minor part of this finding): `IKafkaMessagePublisher.cs` is listed with `defaultTests: []` and reason "Interfaces have no implementation to test" (manifest :30), but lines 66-70 declare `public sealed record KafkaDeliveryResult`, which has 5 coverable lines; set `defaultTests` to `["unit"]` with a reason naming the record.
   - Proposed package `targets`: `unit` 90, `guard` 22, `integration` 60 (provisional).
   - `EncinaKafkaOptions.cs`: unit 100 — 10 initializer lines, no branch.
   - `Health/KafkaHealthCheck.cs`: unit 100 (9/9). guard 33 — constructor lines 27, 29-30 only (3/9 = 33.33%). integration 77 — healthy path against the container (7/9); lines 45 and 47 (the `KafkaException` catch) are covered at unit level.
   - `IKafkaMessagePublisher.cs`: unit 100 — the record's constructor lines 66-70 run in the publisher tests (5/5).
   - `KafkaMessagePublisher.cs`: unit and guard are already proposed by open #1847; this finding amends them (the orchestrator posts the table as a comment on #1847) and adds integration and contract.

   | Flag | #1847 value | Proposed value | Justification |
   | --- | --- | --- | --- |
   | unit | 90 | 100 | `IProducer<string, byte[]>` is substituted (`tests/Encina.UnitTests/Kafka/Publishing/KafkaMessagePublisherTests.cs:18`) and all 103 lines run (103/103); 90 would let 10 lines lose their tests without failing. |
   | guard | 10 | 14 | `ThrowIfNull` at 31-33, 48, 103, 156-157 plus constructor lines (15/103 = 14.56%); 10 tolerates losing four of the 15 reachable lines (11 of 103 suffice). |
   | integration | not in #1847 | 65 (derived, provisional) | success paths against the container; 0/103 today. |
   | contract | not in #1847 | 0 (provisional) | no contract test for the public publisher interface. |
   - `ServiceCollectionExtensions.cs`: unit 100 (derived) — the 14 missed lines (44-56, 58) are the `IProducer<string, byte[]>` factory; `ProducerBuilder.Build()` (line 58) creates the client without contacting a broker, so resolving the producer runs 44-58 including the `Acks` switch (47-53). Today no test runs it: the theory `AddEncinaKafka_WithDifferentAcks_RegistersCorrectly` (`tests/Encina.UnitTests/Kafka/ServiceCollectionExtensionsTests.cs:198-209`) has four rows that only assert a descriptor exists, so `"all"`, `"none"`, `"leader"` and the default arm never execute. guard 39 — 15/38 = 39.47%. No integration target: the factory needs no broker.
   - Open issues: #1847 proposes the publisher's unit and guard targets (amended above); no open issue sets Kafka targets for the other files, the package keys, integration or contract (#1625 covers the scope lifetime, not coverage).

3. **Major** — `.github/coverage-manifest/Encina.NATS.json` (64 lines; `targets` at :5 holds only `guard` 15 and `unit` 55): no file of `Encina.NATS` has per-file targets in the manifest (the only proposal for any of its files is the unit and guard item for `NATSMessagePublisher.cs` in open #1847, amended in the table below); flags that apply: options and validator unit; `Health/NATSHealthCheck.cs` unit, guard, integration; `INATSMessagePublisher.cs` unit (classification defect); `NATSMessagePublisher.cs` unit, guard, integration, contract; `ServiceCollectionExtensions.cs` unit, guard. No package `integration` key although `NATSHealthCheckIntegrationTests` run against `NatsFixture`; package values sit far below the measured 98.78% and 30.53%.
   - Classification defect: `INATSMessagePublisher.cs` is `defaultTests: []` (manifest :37) but lines 63-66 declare `public sealed record NATSPublishAck` (4 coverable lines, executed by `tests/Encina.UnitTests/NATS/NATSPublishAckTests.cs`); set `["unit"]` with a reason naming the record.
   - Proposed package `targets`: `unit` 95, `guard` 30, `integration` 50 (provisional).
   - `EncinaNATSOptions.cs`: unit 100 — 10 initializer lines. `EncinaNATSOptionsValidator.cs`: unit 100 — the validator's branches all run (23/23); guard and property invariants live on the core endpoint validator (#852), as the manifest reason says.
   - `Health/NATSHealthCheck.cs`: unit 100 (10/10). guard 30 — constructor lines only (3/10). integration 70 — the open-connection path runs against the container (7/10); lines 44, 46, 48 (non-open state, `NatsException`) are covered at unit level.
   - `INATSMessagePublisher.cs`: unit 100 — the record constructor lines 63-66 (4/4).
   - `NATSMessagePublisher.cs`: unit and guard are already proposed by open #1847; this finding amends them (the orchestrator posts the table as a comment on #1847) and adds integration and contract.

   | Flag | #1847 value | Proposed value | Justification |
   | --- | --- | --- | --- |
   | unit | 90 | 100 | `INatsConnection` and `INatsJSContext` are substituted (`tests/Encina.UnitTests/NATS/Publishing/NATSMessagePublisherTests.cs:22-23`) and all 89 lines run (89/89), `RequestAsync` (84) included; 90 would let 8 lines lose their tests without failing (81 of 89 suffice). |
   | guard | 15 | 17 | `ThrowIfNull` at 35-37, 52, 92, 151 plus constructor lines (16/89 = 17.98%); 15 tolerates losing two of the 16 reachable lines (14 of 89 suffice). |
   | integration | not in #1847 | 55 (derived, provisional) | `PublishAsync` and `RequestAsync` against the container; `JetStreamPublishAsync` (from line 145) stays unit-only because `NatsFixture` starts `nats:2-alpine` with no JetStream option (`tests/Encina.TestInfrastructure/Fixtures/NatsFixture.cs:33-34`). |
   | contract | not in #1847 | 0 (provisional) | no contract test for the public publisher interface. |
   - `ServiceCollectionExtensions.cs`: unit 100 (derived) — lines 56-57 are the `INatsJSContext` factory; `NatsConnection` connects lazily, so resolving `INatsJSContext` with `UseJetStream = true` runs them. guard 65 — 21/32 = 65.62%.
   - Open issues: #1847 proposes the publisher's unit and guard targets (amended above; its justification cites the `RequestAsync` lines 93-141 as executed, which matches this measurement). #1629 is closed and consistent with this measurement (`NATSMessagePublisher.cs` 89/89, lines 93-141 executed; its 65.2% figure is obsolete). No open issue sets NATS targets for the other files, the package keys, integration or contract.

4. **Major** — `.github/coverage-manifest/Encina.AzureServiceBus.json` (57 lines; `targets` at :5 holds only `guard` 15 and `unit` 55): no file of `Encina.AzureServiceBus` has per-file targets in the manifest (the only proposal for any of its files is the unit and guard item for `AzureServiceBusMessagePublisher.cs` in open #1847, amended in the table below); flags that apply: options unit; `Health/AzureServiceBusHealthCheck.cs` unit, guard, integration; `AzureServiceBusMessagePublisher.cs` unit, guard, integration, contract; `ServiceCollectionExtensions.cs` unit, guard. There is no integration test and no `.md` justification for the package in `tests/Encina.IntegrationTests` (a folder with neither means the flag was not evaluated, `AGENTS.md` section 9); the repository references no Service Bus Testcontainers module (`tests/Encina.TestInfrastructure` references only MsSql, PostgreSql, MySql, MongoDb, RabbitMq, Kafka, Nats, Redis, LocalStack).
   - Proposed package `targets`: `unit` 95, `guard` 32; no package `integration` key until an emulator fixture exists.
   - `EncinaAzureServiceBusOptions.cs`: unit 100 — 10 initializer lines.
   - `Health/AzureServiceBusHealthCheck.cs`: unit 100 (10/10). guard 30 — constructor lines 27, 29-30 (3/10). integration 0 (provisional) — no emulator container is available in the fixtures.
   - `AzureServiceBusMessagePublisher.cs`: unit and guard are already proposed by open #1847; this finding amends them (the orchestrator posts the table as a comment on #1847) and adds integration and contract. Missed unit lines by cause: 69, 111, 157, 188 are the closing braces of the four `try` blocks after a `return` (no statement of their own; likely the implicit disposal of `await using var sender`), 204-205 are `DisposeAsync`, reachable but called by no unit test (the guard run reaches them only through the scope disposal at `tests/Encina.GuardTests/AzureServiceBus/AzureServiceBusGuardTests.cs:135`, in a test, `:119-138`, that asserts only that the services resolve, never the disposal). With the four braces excluded the ceiling is 93/97 = 95.9%.

   | Flag | #1847 value | Proposed value | Justification |
   | --- | --- | --- | --- |
   | unit | 90 | 95 | `ServiceBusClient` and `ServiceBusSender` are substituted (`tests/Encina.UnitTests/AzureServiceBus/Publishing/AzureServiceBusMessagePublisherTests.cs:20-21`); 91/97 = 93.81% today, 95 is reached by one test that disposes the publisher (204-205, 93/97 = 95.9% ceiling); 90 tolerates losing three of the 91 covered lines (88 of 97 suffice). |
   | guard | 15 | 16 | `ThrowIfNull` at 31-33, 47, 89, 132 plus constructor lines and the incidental 204-205 (16/97 = 16.49%); 15 tolerates losing one of the 16 lines (15 of 97 suffice). |
   | integration | not in #1847 | 0 (provisional) | write the `AzureServiceBus.md` justification under `tests/Encina.IntegrationTests/` (`AGENTS.md` section 9 template) naming the missing emulator fixture. |
   | contract | not in #1847 | 0 (provisional) | no contract test for the public publisher interface. |
   - `ServiceCollectionExtensions.cs`: unit 100 (24/24). guard 100 — the guard test builds the provider and covers every line (24/24).
   - Open issues: #1847 proposes the publisher's unit and guard targets (amended above). #939 covers load tests for SQS and Service Bus only; no open issue sets ASB targets for the other files or the package keys, or covers integration.

5. **Major** — `.github/coverage-manifest/Encina.AmazonSQS.json` (64 lines; `targets` at :5 holds only `guard` 15 and `unit` 55): no file of `Encina.AmazonSQS` has per-file targets in the manifest (the only proposal for any of its files is the unit and guard item for `AmazonSQSMessagePublisher.cs` in open #1847, amended in the table below); flags that apply: options and validator unit; `Health/AmazonSQSHealthCheck.cs` unit, guard, integration; `AmazonSQSMessagePublisher.cs` unit, guard, integration, contract; `ServiceCollectionExtensions.cs` unit, guard. `LocalStackFixture` exists (`tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs:14`, `CreateSqsClient` at :37) and no test uses it (its only references are inside its own file), so integration applies and is unmet; there is neither an integration `.cs` nor a `.md` for the package.
   - Proposed package `targets`: `unit` 95, `guard` 21, `integration` 50 (provisional).
   - `EncinaAmazonSQSOptions.cs`: unit 100 — 11 initializer lines. `EncinaAmazonSQSOptionsValidator.cs`: unit 100 — all 20 lines (endpoint-policy branches) run; guard and property invariants live on the core endpoint validator (#852).
   - `Health/AmazonSQSHealthCheck.cs`: unit 100 (14/14: OK status, non-OK status and `AmazonSQSException` catch, lines 46-55). guard 21 — constructor lines 28, 30-31 (3/14 = 21.43%). integration 70 (derived, provisional) — `ListQueuesAsync` against LocalStack covers the OK path (lines 38-48); the other branches stay at unit level.
   - `AmazonSQSMessagePublisher.cs`: unit and guard are already proposed by open #1847; this finding amends them (the orchestrator posts the table as a comment on #1847) and adds integration and contract.

   | Flag | #1847 value | Proposed value | Justification |
   | --- | --- | --- | --- |
   | unit | 90 | 100 | `IAmazonSQS` and `IAmazonSimpleNotificationService` are substituted (`tests/Encina.UnitTests/AmazonSQS/Publishing/AmazonSQSMessagePublisherTests.cs:29-30`) and all 155 lines run (155/155); 90 would let 15 lines lose their tests without failing (140 of 155 suffice). |
   | guard | 10 | 12 | `ThrowIfNull` at 39-42, 57, 112, 167, 246-247 plus constructor lines (19/155 = 12.26%); 10 tolerates losing three of the 19 reachable lines (16 of 155 suffice). |
   | integration | not in #1847 | 60 (derived, provisional) | send, publish, batch and FIFO paths (51, 106, 161, 238) against LocalStack. |
   | contract | not in #1847 | 0 (provisional) | no contract test for the public publisher interface. |
   - `ServiceCollectionExtensions.cs`: unit 68 (derived) — the 10 missed lines (52-56, 61-65) are the bodies of the `IAmazonSQS` and `IAmazonSimpleNotificationService` factories, which construct AWS SDK clients through the default credential chain; they run in a unit test only if the test supplies credentials without shared state. guard 65 — 21/32 = 65.62%. integration 0 (provisional) — the factories set only `RegionEndpoint` (lines 54, 63), so the LocalStack endpoint cannot be applied through them.
   - Open issues: #1847 proposes the publisher's unit and guard targets (amended above). #554 and #939 are load-test issues; no open issue sets SQS targets for the other files or the package keys, or covers integration tests.

6. **Major** — `.github/coverage-manifest/Encina.Redis.PubSub.json` (49 lines; `targets` at :5 holds only `guard` 15 and `unit` 55): no file of `Encina.Redis.PubSub` has per-file targets in the manifest (the only proposal for any of its files is the unit and guard item for `RedisPubSubMessagePublisher.cs` in open #1847, amended in the table below); flags that apply: options unit and property; `RedisPubSubMessagePublisher.cs` unit, guard, integration, contract, property; `ServiceCollectionExtensions.cs` unit, guard, integration. Contract (16 tests) and property (6 tests) exist and measure the package, yet the package has no `contract` or `property` key, so neither flag is gated. The only integration artefact, `tests/Encina.IntegrationTests/Messaging/RedisPubSub/RedisPubSub.md`, is a justification that no longer holds: it says the pipeline has no Redis service in the core profile and recommends creating a `RedisFixture` ("Recommended Alternative", step 1), but `RedisFixture` already exists (`tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs:11`, Testcontainers.Redis) and other integration tests use it (`tests/Encina.IntegrationTests/Infrastructure/Caching/RedisPubSubProviderIntegrationTests.cs` tests the cache `IPubSubProvider`, a different type).
   - Proposed package `targets`: `unit` 75, `guard` 19, `contract` 20, `property` 80, `integration` 70 (provisional).
   - `EncinaRedisPubSubOptions.cs`: unit 100 — 8 initializer lines. property 85 — 7/8 = 87.5% (line 37, `UsePatternSubscription`, is never touched); the lines for `CommandChannel` and `EventChannel` are executed but not asserted (`Property_ToString_NeverThrows` checks only that the `ToString` result is non-empty, and `ToString` reads `ChannelPrefix` alone, `EncinaRedisPubSubOptions.cs:50-51`).
   - `RedisPubSubMessagePublisher.cs`: unit and guard are already proposed by open #1847 (unit 65 "raise to 90 when the 21 uncovered lines are tested"); this finding amends them (the orchestrator posts the table as a comment on #1847) and adds contract, property and integration. The 21 missed unit lines are all the subscription path (`SubscribeAsync` 95-106, `SubscribePatternAsync` 118-129, `RedisSubscription` 172-180); `ChannelMessageQueue` has no public constructor and is sealed, so a substitute `ISubscriber.SubscribeAsync` returns null and `channelQueue.OnMessage` (102, 125) throws: only lines 95-100 and 118-123 can run, with nothing assertable. Delivery helpers (131-158) are executed by `RedisPubSubDeliveryTests`.

   | Flag | #1847 value | Proposed value | Justification |
   | --- | --- | --- | --- |
   | unit | 65 | 70 (derived) | `IConnectionMultiplexer` and `ISubscriber` are substituted (`tests/Encina.UnitTests/Messaging/RedisPubSub/RedisPubSubMessagePublisherTests.cs:21-23`); 50/71 = 70.42% today; of the 21 missed lines only 95-100 and 118-123 (8 lines) can run against a substitute and nothing is assertable there (above), so 70 holds the measured value; 65 tolerates losing three of the 50 covered lines (47 of 71 suffice). #1847's "raise to 90 later" is not reachable by unit tests through the sealed `ChannelMessageQueue` and belongs to the integration flag. |
   | guard | 20 | 23 | `ThrowIfNull` at 34-36, 51, 93, 115-116 plus constructor lines (17/71 = 23.94%); 20 tolerates losing two of the 17 lines (15 of 71 suffice). |
   | integration | not in #1847 | 75 (derived, provisional) | subscribe, pattern subscribe, deliver and dispose against `RedisFixture`; the `RedisPubSub.md` justification is stale (finding header). |
   | contract | not in #1847 | 23 | 17/71, the same constructor and null-check lines, run by the four tests that instantiate the publisher (`tests/Encina.ContractTests/Messaging/RedisPubSub/RedisPubSubPublisherContractTests.cs:224, :246, :268`, plus the one at :291); the remaining contract tests are reflection-only and cover no line. |
   | property | not in #1847 | 0 (provisional) | no property test touches the publisher; the `RedisMessageWrapper` serialize-and-deliver round trip (161-166, 131-150) is the natural invariant. |
   - `ServiceCollectionExtensions.cs`: unit 81 — lines 40-43 are the `IConnectionMultiplexer` factory whose `ConnectionMultiplexer.Connect` (43) is a synchronous connect to a reachable server, so they belong to integration; 18/22 = 81.82%. guard 4 — only the `ThrowIfNull` at line 22 (1/22 = 4.55%). integration 90 (derived, provisional) — building the provider with `AddEncinaRedisPubSub` against `RedisFixture` runs the whole method.
   - Open issues: #1847 proposes the publisher's unit and guard targets (amended above); no open issue sets targets for the other Redis.PubSub files, the package keys, contract, property or integration. #1389 lists `Encina.Redis.PubSub` guard at 12.9% against 15; this stage measures 19.35% (18/93, manifest-applicable files), so that row is stale or counted differently (see Informational). #941 is the benchmark issue.

7. **Major** — `.github/coverage-manifest/Encina.InMemory.json` (46 lines; `targets` at :5 holds only `guard` 15 and `unit` 55): the entry of `InMemoryMessageBus.cs` (`:27-30`) is `defaultTests: []` with reason "Interface — no implementation", copied from the entry of the real interface file `IInMemoryMessageBus.cs` (`:22`); the file is `public sealed class InMemoryMessageBus` (`src/Encina.InMemory/InMemoryMessageBus.cs:14`) with 110 coverable lines. Because of it the package fails its own targets today: unit 22.22% (4/18) against 55, and guard has no test at all (`tests/Encina.GuardTests` has no InMemory folder; the package is absent from the guard report) against 15. Flags that apply: options unit; `InMemoryMessageBus.cs` unit, guard, contract; `ServiceCollectionExtensions.cs` unit, guard. Integration: in-process, so a justification `.md` is required (`AGENTS.md` section 9) and none exists under `tests/Encina.IntegrationTests`.
   - Proposed package `targets`: `unit` 90, `guard` 4 (4.88% reachable by null-check tests on this package, derived below).
   - `EncinaInMemoryOptions.cs`: unit 100 — line 21 (the `FullMode` getter) runs only when the bounded channel is created, and every test sets `UseUnboundedChannel = true` (`tests/Encina.UnitTests/InMemory/InMemoryMessageBusTests.cs:167-168`, against the default `false` at `EncinaInMemoryOptions.cs:16`), so the default configuration has no unit coverage at all; one test with default options runs it (5/5).
   - `InMemoryMessageBus.cs`: set `defaultTests` to `["unit", "guard"]` with a reason naming the class. unit 95 — no external dependency, the real bus runs under a `FakeLogger` (`InMemoryMessageBusTests.cs:167-168`). Missed lines by cause: 49-55 and 57-63 are the default bounded-channel branch and the `FullMode` switch (reachable with default options; also leaves `DropOldest`, `DropNewest` and `Wait` untested); 76 is the `PendingCount` getter (reachable); 174 is the end of the read loop (unreachable: `Dispose` cancels the token first, so `ReadAllAsync` throws) and 193, 195-196 are the outer `catch` of `ProcessSingleMessageAsync`, unreachable because `InvokeHandlerAsync` already swallows handler exceptions (209-212). Ceiling 106/110 = 96.36%. guard 4 (derived) — null-check guard tests reach 5 lines: constructor 32-33, `PublishAsync` 87, `EnqueueAsync` 126, `Subscribe` 155 (5/110 = 4.55%); a guard test file must be added. contract 0 (provisional) — no contract test of `IInMemoryMessageBus`.
   - `ServiceCollectionExtensions.cs`: unit 100 (derived) — all 13 lines are options copy and `TryAddSingleton`; resolving `IOptions<EncinaInMemoryOptions>` and `IInMemoryMessageBus` runs lines 21-35. Today the only caller of `AddEncinaInMemory` under `tests/` is the NBomber factory (`tests/Encina.NBomber/Scenarios/Messaging/Providers/InMemoryBusProviderFactory.cs:47`), so unit is 0/13. guard 7 — `ThrowIfNull` at line 21 (1/13 = 7.69%).
   - Open issues: #1626 describes this package; its claim "no tests of any type" and its figures (0/110, 0/5, 0/13) are stale for the bus and options (tests landed with #1606: 91/110 and 4/5), because the test file builds the bus with a target-typed `new(...)` (`InMemoryMessageBusTests.cs:167-168`), which a search for `new InMemoryMessageBus(` misses. Still valid in #1626: the manifest entry, the registration at 0/13 and the missing guard tests. The bounded default path (49-63) is not named by #1626.

8. **Major** — `.github/coverage-manifest/Encina.MQTT.json` (40 lines; `targets` at :6 holds only `guard` 15 and `unit` 55): no file of `Encina.MQTT` has per-file targets in the manifest (the only proposal for any of its files is the unit and guard item for `MQTTMessagePublisher.cs` in open #1847, amended in the table below); flags that apply: options unit; `Health/MQTTHealthCheck.cs` unit, guard, integration; `MQTTMessagePublisher.cs` unit, guard, integration, contract; `ServiceCollectionExtensions.cs` unit, guard, integration. No package `integration` key although `MQTTHealthCheckIntegrationTests` run against `MqttFixture`; the package unit value sits well below the measured 72.90%.
   - Proposed package `targets`: `unit` 70, `guard` 21, `integration` 60 (provisional).
   - `EncinaMQTTOptions.cs`: unit 100 — 12 initializer lines.
   - `Health/MQTTHealthCheck.cs`: unit 100 (7/7). guard 42 — constructor lines 27, 29-30 (3/7 = 42.86%). integration 100 — both the connected and the disconnected branch run against the container (7/7).
   - `MQTTMessagePublisher.cs`: unit and guard are already proposed by open #1847 (unit 65, "raise the unit target to 90 when they are tested"; guard 10); this finding amends them (the orchestrator posts the table as a comment on #1847) and adds integration and contract. The 44 missed lines split by cause. (a) `SubscribeAsync` 108-129 and `SubscribePatternAsync` 142-163: lines 108-124 and 142-158 (QoS switch, subscribe options, `_client.SubscribeAsync`) are reachable with the substituted `IMqttClient` the existing tests use (`tests/Encina.UnitTests/MQTT/Publishing/MQTTMessagePublisherTests.cs:19`, the subscribe tests at :299-354 stub it at :306 but stop at the null guards); lines 126 and 160 cast `(MqttClient)_client`, which throws `InvalidCastException` for a substitute, so 128-129 and 162-163 need a real connected client (integration) or a seam. (b) `DisposeAsync` of the two subscription classes, 220-227 and 307-314: reachable on the real never-connected `MqttClient` of `tests/Encina.UnitTests/MQTT/Publishing/MqttSubscriptionTests.cs:20` (no test disposes a subscription); `UnsubscribeAsync` throws on a client that is not connected, so the closing braces 227 and 314 stay out. Ceiling (146-6)/146 = 95.9%.

   | Flag | #1847 value | Proposed value | Justification |
   | --- | --- | --- | --- |
   | unit | 65 | 90 (derived, provisional) | 102/146 = 69.86% today; 65 tolerates losing seven of the 102 covered lines (95 of 146 suffice) and gives the 44 missed lines no obligation. 90 is the post-#1627 value (ceiling 95.9% after the subscribe and disposal tests, 90 leaves room because it was not run); set 65 only until those tests land and 90 with them, since 90 fails today's 69.86%. |
   | guard | 10 | 10 | `ThrowIfNull` at 32-34, 53, 105-106, 139-140 plus constructor lines (16/146 = 10.96%); same value as #1847 (15 of 146 suffice, one line of slack). |
   | integration | not in #1847 | 65 (derived, provisional) | publish, subscribe, pattern subscribe and subscription disposal against `MqttFixture`; 0/146 today. |
   | contract | not in #1847 | 0 (provisional) | no contract test for the public publisher interface. |
   - `ServiceCollectionExtensions.cs`: unit 71 — the 14 missed lines (45-46, 48-52, 54, 56, 59, 61, 64, 67, 69) are all inside the `IMqttClient` factory, which connects at line 67 and so runs only against a reachable broker; 35/49 = 71.43%. guard 51 — 25/49 = 51.02%. integration 90 (derived, provisional) — resolving `IMqttClient` from `AddEncinaMQTT` against `MqttFixture` runs 45-69 including the credential and TLS branches only if the test sets them.
   - Open issues: #1847 proposes the publisher's unit and guard targets (amended above). #1627 is stale: it states 33.6% (49/146) and uncovered lines 107-162 and 184-313; today `MQTTMessagePublisher.cs` is 69.86% (102/146) because `MqttSubscriptionTests.cs` executes the subscription classes' receive and match code (184-306). Refreshed state for the #1627 retarget (measured here in Release and reproduced by the verifier, pass 1): 69.86% (102/146); missed lines 108-129, 142-163 (the `SubscribeAsync` and `SubscribePatternAsync` bodies), 220-227 and 307-314 (the two subscription classes' `DisposeAsync`); the casts `(MqttClient)_client` at `src/Encina.MQTT/MQTTMessagePublisher.cs:126` and `:160` are what keep 128-129 and 162-163 behind a connected client, so #1627's success criterion ("covers lines 107-162 and 184-313") should be retargeted to the 44 missed lines above, of which 128-129 and 162-163 need a connected client (integration) or a seam; the unit target proposed for the file is 90 (table above). #1628 is still accurate for this file's registration (35/49; its range 45-69 matches the missed set).

9. **Major** — `.github/coverage-manifest/Encina.gRPC.json` (70 lines; `targets` at :5 holds only `guard` 15 and `unit` 55): no file of `Encina.gRPC` has per-file targets in the manifest (the only proposal for any of its files is the unit and guard item for `GrpcMediatorService.cs` in open #1847, amended in the table below); flags that apply: `CachingTypeResolver.cs` unit, guard; options unit; `GrpcMediatorService.cs` unit, guard, contract; `Health/GrpcHealthCheck.cs` unit, guard; `ServiceCollectionExtensions.cs` unit, guard. There is no integration test and no `.md` for the package under `tests/Encina.IntegrationTests`.
   - Proposed package `targets`: `unit` 90, `guard` 23.
   - `CachingTypeResolver.cs`: unit 100 (7/7). guard 100 — the two checks (16, 23) and the constructor run (7/7).
   - `EncinaGrpcOptions.cs`: unit 100 — 7 initializer lines.
   - `GrpcMediatorService.cs` (declares `GrpcEncinaService`, `GrpcSerializationException`): unit and guard are already proposed by open #1847; this finding amends them (the orchestrator posts the table as a comment on #1847) and adds integration and contract. The 17 missed unit lines are four defensive branches that no public-API input reaches: 178-181 and 214-217 (`IEncina` without a generic `Send` or `Publish`: `FindGenericMethod` searches `typeof(IEncina)` itself at 241-244, so a substitute cannot remove the method), 319-323 (a `Right` whose value is null) and 345-348 (a `Left` whose error is null), neither of which a LanguageExt `Either` can hold; 145/162 = 89.51% is the ceiling for tests through the public API.

   | Flag | #1847 value | Proposed value | Justification |
   | --- | --- | --- | --- |
   | unit | 85 | 89 | `IEncina` is substituted (`tests/Encina.UnitTests/gRPC/GrpcEncinaServiceTests.cs:20`, `GrpcEncinaServiceDispatchTests.cs:22`) and the measured value equals the public-API ceiling (145/162 = 89.51%); 85 tolerates losing seven of the 145 covered lines (138 of 162 suffice). |
   | guard | 10 | 12 | `ThrowIfNull` at 35-38, 52-53, 107-108, the exception constructor at 367, 369 and constructor lines (20/162 = 12.35%); 10 tolerates losing three of the 20 lines (17 of 162 suffice). |
   | integration | not in #1847 | 0 (provisional) | the gRPC transport is in-process (`Grpc.AspNetCore` on a test server) and no integration test exists; write `gRPC.md` under `tests/Encina.IntegrationTests/` or a test server test. |
   | contract | not in #1847 | 0 (provisional) | no contract test for the public service. |
   - `Health/GrpcHealthCheck.cs`: unit 100 (10/10). guard 30 — constructor lines 36, 38-39 (3/10). integration 0 (provisional) — same justification as the service.
   - `ServiceCollectionExtensions.cs`: unit 100 (16/16). guard 100 — the guard test builds the provider with `ValidateOnBuild` and `ValidateScopes` (`tests/Encina.GuardTests/gRPC/GrpcGuardTests.cs:200-201`), 16/16.
   - Open issues: #1847 proposes the publisher's unit and guard targets (amended above); no open issue sets gRPC targets for the other files, the package keys, integration or contract.

10. **Major** — `.github/coverage-manifest/Encina.GraphQL.json` (52 lines; `targets` at :6 holds only `guard` 15 and `unit` 55): no file of `Encina.GraphQL` has per-file targets in the manifest (the only proposal for any of its files is the unit and guard item for `GraphQLMediatorBridge.cs` in open #1847, amended in the table below); flags that apply: options unit; `GraphQLMediatorBridge.cs` unit, guard, contract; the four `Pagination/` files unit; `ServiceCollectionExtensions.cs` unit, guard. There is no integration test and no `.md` for the package under `tests/Encina.IntegrationTests`.
   - Proposed package `targets`: `unit` 85, `guard` 26.
   - `EncinaGraphQLOptions.cs`: unit 100 — 8 initializer lines.
   - `GraphQLMediatorBridge.cs` (declares `GraphQLEncinaBridge`): unit and guard are already proposed by open #1847; this finding amends them (the orchestrator posts the table as a comment on #1847) and adds integration and contract. The 10 missed unit lines are the two timeout catches, `QueryAsync` 63, 65-68 and `MutateAsync` 105, 107-110, reachable by a substituted `Send` that throws `OperationCanceledException` while the caller's token is not cancelled (the `when` filter at 63 and 105) and asserts `GRAPHQL_TIMEOUT`.

   | Flag | #1847 value | Proposed value | Justification |
   | --- | --- | --- | --- |
   | unit | 80 | 100 (derived, provisional) | `IEncina` is substituted (`tests/Encina.UnitTests/GraphQL/GraphQLEncinaBridgeTests.cs:14`); 58/68 = 85.29% today and the 10 missed lines are all reachable by one timeout test per method (above); 80 tolerates losing three of the 58 covered lines (55 of 68 suffice) and leaves the timeout catches unprotected. 100 fails today's 85.29% until those two tests exist, so 80 holds only until then. |
   | guard | 15 | 20 | `ThrowIfNull` at 31-33, 46, 88, 130 plus constructor lines (14/68 = 20.59%); 15 tolerates losing three of the 14 lines (11 of 68 suffice). |
   | integration | not in #1847 | 0 (provisional) | in-process Hot Chocolate bridge, no integration test; justification `.md` required. |
   | contract | not in #1847 | 0 (provisional) | no contract test for the public bridge. |
   - `Pagination/Connection.cs` unit 100 (27/27), `Pagination/ConnectionExtensions.cs` unit 100 (40/40), `Pagination/Edge.cs` unit 100 (2/2), `Pagination/RelayPageInfo.cs` unit 100 (11/11) — pure data transformation and records, fully executed by `tests/Encina.UnitTests/GraphQL/Pagination/*Tests.cs`; the `internal` null checks (`Connection.cs:131`, `ConnectionExtensions.cs:47`) are exercised at unit level only, which is consistent with their unit-only entries.
   - `ServiceCollectionExtensions.cs`: unit 100 (derived) — all 16 lines are registration; the only test of `AddEncinaGraphQL` lives in the guard class (`tests/Encina.GuardTests/GraphQL/GraphQLGuardTests.cs:211`) and never resolves `IOptions<EncinaGraphQLOptions>`, so the `Configure` lambda (28-35) runs in no flag and unit is 0/16; a unit test that resolves the options and the bridge runs every line. guard 50 — 8/16 (21, 23-24, 26-27, 36, 38, 40).
   - Open issues: #1847 proposes the publisher's unit and guard targets (amended above); no open issue sets GraphQL targets for the other files, the package keys, integration or contract.

## Informational (not findings)

- How to read the findings: every one of the ten manifests has only the package-level `targets` (`guard` 15, `unit` 55; `targets` at line 5 or 6) and per-file `defaultTests`, `defaultRule` and `reason`; none has a per-file `targets` or `justifications` key (`Select-String -Pattern '"justifications"'` over the ten files returns nothing). Open #1847 (delta re-audit of #3, item "Per-file coverage targets and justifications for nine transport and API bridge publishers") already proposes unit and guard targets for nine publisher files (one per package except InMemory); findings 1-6 and 8-10 amend those values in a per-publisher table (the orchestrator posts the tables as a comment on #1847) and keep as their own scope the package keys, the integration and contract flags, the non-publisher files and the classification defects. A search of the bodies of all 963 open issues (2026-10-06) found no other issue that sets per-file targets for a file of these ten packages: #1625, #1626, #1627 and #1628 state that the package targets are aggregates and compute no per-file gap. Each finding proposes the package `targets` keys and, for every file with a coverable line, per-file targets with one-sentence justifications (schema: `docs/testing/coverage-measurement-methodology.md`, section "Per-file targets and justifications"). Values marked "derived" were reasoned from the code and from the mocks the existing tests use, not run (this stage writes no tests): provisional until a test run confirms them. A target of 0 is proposed only where `AGENTS.md` section 9 requires the flag and no test exists yet, and is provisional by that rule.
- Package-level result today: every package passes its `unit` 55 and `guard` 15 aggregate except `Encina.InMemory` (unit 22.22% and no guard test), whose cause is the `InMemoryMessageBus.cs` classification covered in finding 7. The aggregates are far above the targets for the other nine, which is why findings 1-10 propose raising them.
- Flags with no test for the nine non-Redis packages: contract and property (not measured: no test exists); integration for Azure Service Bus, Amazon SQS, gRPC, GraphQL, InMemory and Redis.PubSub (no `.cs`; only Redis has a `.md`, stale, see finding 6).
- Load and benchmark: `tests/Encina.LoadTests` and `tests/Encina.BenchmarkTests` hold only `Messaging/RedisPubSub/RedisPubSub.md` justifications for these packages; NBomber scenarios exist for Kafka, MQTT, NATS, RabbitMQ and InMemory under `tests/Encina.NBomber/Scenarios/Brokers` and `.../Messaging`. Open: #552, #554, #555, #556, #939, #941, #944. Not rule (b) material.
- Registration completeness (`AGENTS.md` section 3): of the ten `AddEncina*` methods only `AddEncinaAmazonSQS` (`tests/Encina.UnitTests/AmazonSQS/ServiceCollectionExtensionsEndpointValidationTests.cs:38-45`), `AddEncinaNATS` (`tests/Encina.UnitTests/NATS/ServiceCollectionExtensionsTests.cs:127,142`, `AddEncinaNATS_ValidOptions_ProviderBuildsWithValidateOnBuildAndScopes`; it never resolves `INatsJSContext`, which is why lines 56-57 stay missed) and the gRPC one (`tests/Encina.GuardTests/gRPC/GrpcGuardTests.cs:200-201`) are built with `ValidateOnBuild`; the architecture check for all of them is #1308 (open).
- The contract tests of Redis.PubSub are mostly reflection-only (`typeof(IRedisPubSubMessagePublisher).GetMethod(...)`, 12 of 16 tests) and add 17 covered lines only through four instantiating tests; a test-quality matter outside this delta.
- `GrpcMediatorService.cs` declares `GrpcEncinaService` and `GraphQLMediatorBridge.cs` declares `GraphQLEncinaBridge` (file names differ from the declared types); the health checks of RabbitMQ, Kafka, NATS, Azure Service Bus, SQS, MQTT and gRPC assign `serviceProvider` without a null check, so their guard tests reach only the constructor (code-stage matter).
- Open-issue comparison (original audit): #1847 (item for nine publishers) sets unit and guard values for the publisher files, all at or below the measurement and below the values proposed here (RabbitMQ 90/20, Kafka 90/10, NATS 90/15, Azure Service Bus 90/15, SQS 90/10, Redis.PubSub 65/20, MQTT 65/10, gRPC 85/10, GraphQL 80/15); its measured values match this stage's figures (verified); #1626 stale for the bus and options (finding 7); #1627 stale (finding 8); #1628 accurate for both files, with one refinement (finding 1); #1629 closed and consistent (finding 3); #1389 row for Redis.PubSub (12.9%) differs from this measurement (19.35% on manifest-applicable files, 23.76% over all files) and should be recomputed by the orchestrator; #1625 and #1608 are scope-lifetime issues outside rule (b).
- Integration run detail: `RabbitMQHealthCheckIntegrationTests` 3, `KafkaHealthCheckIntegrationTests` 3, `NATSHealthCheckIntegrationTests` 3, `MQTTHealthCheckIntegrationTests` 4, all passed against Testcontainers (`RabbitMqFixture`, `KafkaFixture`, `NatsFixture`, `MqttFixture`); they exercise no publisher (publisher integration 0% in all four).
- Derived ceilings in the findings (Kafka `Build()`, NATS lazy connect, RabbitMQ unreachable-host resolution, MQTT disposal on an unconnected client, SQS credential chain, Redis sealed `ChannelMessageQueue`) come from reading the code and the libraries' behaviour, not from a run.

## CRAP

Pending #1346 (the CRAP gate is not computed in this stage).

## Lessons for the pipeline

- A package that has tests of a flag but no package `targets` key for it (Redis.PubSub contract and property) is silently ungated: the dashboard cell has no target to color against. Rule (b) should compare the flags that have a report with the `targets` keys, not only the files' `defaultTests`.
- A manifest `reason` copied from a neighbour ("Interface — no implementation" on `InMemoryMessageBus.cs`) made the package fail its own unit target without anyone noticing; the manifest check should flag any `defaultTests: []` entry whose file contains a non-interface type (it also catches `IKafkaMessagePublisher.cs` and `INATSMessagePublisher.cs`, which declare records).
- A search for `new <Type>(` misses target-typed `new(...)` helpers (`CreateBus` at `InMemoryMessageBusTests.cs:167-168`); open issues that claim "no test constructs X" from such a search go stale or were wrong, so every such claim needs the measured line count next to it.
- A class that flips a default for all its tests (`UseUnboundedChannel = true`) leaves the default path with zero unit coverage and shows up as a getter line missed in the options file (`EncinaInMemoryOptions.cs:21`); in rule (b) read the missed options lines first, they point at untested defaults.
- A test theory named for behaviour that asserts only that a descriptor exists (`AddEncinaKafka_WithDifferentAcks_RegistersCorrectly`) leaves the code it names unexecuted; the missed-line list of the registration file is the quickest way to find such tests.
- A claim "no open issue sets targets" needs a search of the open issue bodies, not titles: the per-file targets for the nine publishers sat as one item inside the consolidated delta umbrella #1847 ("Delta re-audit ... of #3"), which no title search of this stage found (verifier pass 1 correction 2). Search `gh issue list --state open --json number,title,body` for the manifest names and "per-file" before writing "none".
- A "only X and Y are built with `ValidateOnBuild`" claim needs a search of every registration test over all test projects; the first version missed `tests/Encina.UnitTests/NATS/ServiceCollectionExtensionsTests.cs:127,142`. List each hit with file:line and say "N of 10", never "only".
- A "tolerates losing N lines" statement must be computed as coverable lines minus ceil(target x coverable); the first draft of the per-publisher tables mis-counted the slack by one in four rows. Print both numbers next to the claim.
- The hook `block-main-checkout-writes` refused `Set-Content` for a scratch `.ps1` ("repo source files ... are edited only with the Edit or Write tools, never with PowerShell -replace, Set-Content ..."); the script was written with the Write tool, as the message directs. A scratch script for per-flag coverage parsing is useful to every test stage and could live in `tools/ai/audit/`.
