---
title: "EventId block registry"
layout: default
nav_exclude: true
---

# EventId block registry

This is a reference page (Diátaxis: reference) for a contributor or agent who needs to know which EventId ranges exist, which blocks plans have reserved and where the next block goes. The rule that governs it is the "10000+ zone" amendment of [ADR-021](../architecture/adr/021-eventid-uniqueness-enforcement.md); the summary for day-to-day work is section 7 of [AGENTS.md](../../AGENTS.md).

The page is read by a test. `tests/Encina.UnitTests/Testing/Architecture/EventIdRegistryAgreementTests.cs` parses the two tables whose header rows are `Name | Block | Area | Zone` and `Block | Area | Owner | Status`, and fails when they disagree with `src/Encina/Diagnostics/EventIdRanges.cs`. Keep one row per line and no pipe character inside a cell. Every other table on this page has a different header and is not parsed.

The git-ignored snapshot `artifacts/decisions/plan-batch/eventid-blocks.md` was the source of this registry. It is not the registry any more; this page is.

## The five agreement rules

`EventIdRegistryAgreementTests` enforces these rules on every run of `Encina.UnitTests`:

1. Every range in `EventIdRanges.cs` appears in the registered table with the same bounds.
2. Every registry row with status `registered` matches a range in `EventIdRanges.cs` with the same bounds.
3. Reserved blocks (status `reserved` and `reserved-pre-zone`) do not overlap any registered range.
4. Reserved blocks do not overlap each other.
5. Nothing is below 10000 unless it is a historical registered range (Zone `historical`) or a block with status `reserved-pre-zone`. A range is historical only when its exact name is in the `HistoricalRangeNames` allow-list of `tests/Encina.UnitTests/Testing/Architecture/EventIdRegistryAgreementTests.cs`; the Zone label alone is not trusted, and adding a name is a visible code change. Free gaps below 10000 are not listed on this page and are not allocatable; this page is the place to read free space only for the 10000+ zone.

## How to add a block

1. Pick the next free 50 or 100 id slot in the current 1000 block of your area (area bases and the pick rule are in the ADR-021 amendment); open the next 1000 block of the area when the current one is full.
2. Add a row to the "Blocks" table with status `reserved` (a plan) or `registered` (code).
3. When the code lands, register the range in `EventIdRanges.cs` in the same pull request, add the row to the "Registered ranges" table with Zone `zone`, and set the block row to `registered`.
4. Run `dotnet test tests\Encina.UnitTests\Encina.UnitTests.csproj --filter "FullyQualifiedName~EventIdRegistryAgreementTests"` and the architecture tests (`EncinaEventIdAllocationTests`).

## Registered ranges

Every field of `EventIdRanges.cs`. Zone is `historical` below 10000 and `zone` in the 10000+ zone. The Area column of a historical range is the provisional target area for the migration into the zone scheme (issue #4007), not a promise.

| Name | Block | Area | Zone |
| --- | --- | --- | --- |
| Sanitization | 1-99 | Security and compliance | historical |
| Core | 100-199 | Core and messaging | historical |
| AspNetCore | 200-249 | Observability and platform | historical |
| Testing | 250-299 | Testing and tooling | historical |
| DomainModelingRepository | 1100-1199 | Core and messaging | historical |
| DomainModelingUnitOfWork | 1200-1299 | Core and messaging | historical |
| DomainModelingBulkOperations | 1300-1399 | Core and messaging | historical |
| DomainModelingSpecification | 1400-1499 | Core and messaging | historical |
| EntityFrameworkCoreSoftDelete | 1500-1599 | Database | historical |
| DomainModelingAudit | 1600-1699 | Core and messaging | historical |
| SecurityAuditRead | 1700-1799 | Security and compliance | historical |
| Tenancy | 1800-1899 | Core and messaging | historical |
| ModuleIsolation | 1900-1999 | Core and messaging | historical |
| MessagingOutbox | 2000-2099 | Core and messaging | historical |
| MessagingInbox | 2100-2199 | Core and messaging | historical |
| MessagingSaga | 2200-2299 | Core and messaging | historical |
| MessagingScheduling | 2300-2399 | Core and messaging | historical |
| EntityFrameworkCoreQueryCache | 2400-2449 | Database | historical |
| MessagingEncryption | 2450-2499 | Core and messaging | historical |
| DomainModelingDomainEvents | 2500-2549 | Core and messaging | historical |
| AuditMarten | 2550-2599 | Core and messaging | historical |
| Marten | 2600-2799 | Core and messaging | historical |
| Messaging | 2800-2999 | Core and messaging | historical |
| EntityFrameworkCore | 3000-3099 | Database | historical |
| MongoDB | 3100-3199 | Database | historical |
| ADOSqlServer | 3200-3249 | Database | historical |
| ADOPostgreSQL | 3250-3299 | Database | historical |
| ADOMySQL | 3300-3349 | Database | historical |
| DapperSqlServer | 3350-3399 | Database | historical |
| DapperPostgreSQL | 3400-3449 | Database | historical |
| DapperMySQL | 3450-3499 | Database | historical |
| Caching | 3500-3549 | Caching | historical |
| CachingMemory | 3550-3599 | Caching | historical |
| CachingRedis | 3600-3649 | Caching | historical |
| CachingHybrid | 3650-3699 | Caching | historical |
| DistributedLockInMemory | 3700-3749 | Caching | historical |
| DistributedLockRedis | 3750-3799 | Caching | historical |
| DistributedLockSqlServer | 3800-3849 | Caching | historical |
| TenancyAspNetCore | 3850-3899 | Core and messaging | historical |
| Polly | 3900-3949 | Observability and platform | historical |
| ExtensionsResilience | 3950-3999 | Observability and platform | historical |
| Hangfire | 4000-4049 | Observability and platform | historical |
| Quartz | 4050-4099 | Observability and platform | historical |
| RabbitMQ | 4100-4149 | Transports | historical |
| Kafka | 4150-4199 | Transports | historical |
| NATS | 4200-4249 | Transports | historical |
| MQTT | 4250-4299 | Transports | historical |
| AzureServiceBus | 4300-4349 | Transports | historical |
| AmazonSQS | 4350-4399 | Transports | historical |
| RedisPubSub | 4400-4449 | Transports | historical |
| InMemory | 4450-4499 | Transports | historical |
| GRpc | 4500-4549 | Transports | historical |
| GraphQL | 4550-4599 | Transports | historical |
| SignalR | 4600-4649 | Transports | historical |
| Refit | 4650-4699 | Cloud and integration | historical |
| AwsLambda | 4700-4749 | Cloud and integration | historical |
| AzureFunctions | 4750-4799 | Cloud and integration | historical |
| Cdc | 4800-4899 | Core and messaging | historical |
| CdcSqlServer | 4900-4949 | Core and messaging | historical |
| CdcDebezium | 4950-4999 | Core and messaging | historical |
| SecurityAudit | 5000-5099 | Security and compliance | historical |
| SecuritySecrets | 5100-5199 | Security and compliance | historical |
| SecuritySecretsAzureKeyVault | 5200-5249 | Security and compliance | historical |
| SecuritySecretsAwsSecretsManager | 5250-5299 | Security and compliance | historical |
| SecuritySecretsHashiCorpVault | 5300-5349 | Security and compliance | historical |
| SecuritySecretsGoogleCloudSecretManager | 5350-5399 | Security and compliance | historical |
| CdcMySql | 5400-5449 | Core and messaging | historical |
| MessagingRecoverability | 5450-5499 | Core and messaging | historical |
| OpenTelemetry | 7000-7099 | Observability and platform | historical |
| Security | 8000-8009 | Security and compliance | historical |
| SecurityPII | 8010-8029 | Security and compliance | historical |
| IdGeneration | 8030-8099 | Core and messaging | historical |
| ComplianceGDPR | 8100-8199 | Security and compliance | historical |
| ComplianceConsent | 8200-8299 | Security and compliance | historical |
| ComplianceDSR | 8300-8349 | Security and compliance | historical |
| ComplianceLawfulBasis | 8350-8399 | Security and compliance | historical |
| ComplianceAnonymization | 8400-8449 | Security and compliance | historical |
| MartenGDPRCryptoShredding | 8450-8499 | Security and compliance | historical |
| ComplianceRetention | 8500-8599 | Security and compliance | historical |
| ComplianceDataResidency | 8600-8699 | Security and compliance | historical |
| ComplianceBreachNotification | 8700-8799 | Security and compliance | historical |
| ComplianceDPIA | 8800-8899 | Security and compliance | historical |
| CompliancePrivacyByDesign | 8900-8949 | Security and compliance | historical |
| SecurityABAC | 9000-9099 | Security and compliance | historical |
| SecurityAntiTampering | 9100-9199 | Security and compliance | historical |
| ComplianceNIS2 | 9200-9299 | Security and compliance | historical |
| ComplianceCrossBorderTransfer | 9300-9399 | Security and compliance | historical |
| ComplianceProcessorAgreements | 9400-9499 | Security and compliance | historical |
| ComplianceAIAct | 9500-9599 | Security and compliance | historical |
| ComplianceAttestation | 9600-9699 | Security and compliance | historical |
| SecurityBreakTheGlass | 9750-9799 | Security and compliance | historical |

## Blocks

Every block a plan reserved. Blocks below 10000 were recorded before the 10000+ zone existed and keep their status `reserved-pre-zone`; blocks recorded from now on are born in the zone. Rows are sorted by block. The Owner column holds the plan family, the ids it uses and the date of the assignment.

| Block | Area | Owner | Status |
| --- | --- | --- | --- |
| 300-349 | Caching | wave 3 (plan-batch-2): cache-security-invalidation Note: assigned 2026-11-15 (300-1099 free per AGENTS.md section 7) | reserved-pre-zone |
| 350-399 | Caching | wave 3 (plan-batch-2): cache-stores-1 Note: assigned | reserved-pre-zone |
| 400-449 | Caching | wave 3 (plan-batch-2): cache-stores-2 Note: assigned | reserved-pre-zone |
| 450-499 | Caching | wave 3 (plan-batch-2): cache-patterns-1 Note: assigned | reserved-pre-zone |
| 500-549 | Caching | wave 3 (plan-batch-2): cache-patterns-2 Note: assigned | reserved-pre-zone |
| 550-599 | Security and compliance | wave 3 (plan-batch-2): validation-1 Note: assigned | reserved-pre-zone |
| 600-649 | Security and compliance | wave 3 (plan-batch-2): validation-2 Note: assigned | reserved-pre-zone |
| 650-699 | Security and compliance | wave 3 (plan-batch-2): validation-3 Note: assigned | reserved-pre-zone |
| 700-749 | Observability and platform | wave 3 (plan-batch-2): guards-1 Note: assigned | reserved-pre-zone |
| 750-799 | Observability and platform | wave 3 (plan-batch-2): guards-2 Note: assigned | reserved-pre-zone |
| 800-849 | Observability and platform | wave 3 (plan-batch-2): aspire-1 Note: assigned | reserved-pre-zone |
| 850-899 | Observability and platform | wave 3 (plan-batch-2): aspire-2 Note: assigned | reserved-pre-zone |
| 900-949 | Observability and platform | wave 3 (plan-batch-2): resilience-patterns-1 Note: assigned | reserved-pre-zone |
| 950-999 | Observability and platform | wave 3 (plan-batch-2): resilience-patterns-2 Note: assigned | reserved-pre-zone |
| 1000-1049 | Observability and platform | wave 3 (plan-batch-2): resilience-otel-misc Note: assigned | reserved-pre-zone |
| 1050-1099 | Transports | wave 3 (plan-batch-2): transport-new-1 Note: assigned | reserved-pre-zone |
| 5500-5799 | Security and compliance | platform-spec002 (#1234 5500-5549, #1235 5550-5699, #1236 5700-5749, #1238 5750-5799) Note: proposed in PRs #3503-#3506 | reserved-pre-zone |
| 5800-5849 | Core and messaging | msg-idempotency-saga-inbox (#3440 5800-5804, #1220 5805-5810, #1221 5811-5816, #2385 5817-5822; 5823-5849 free) Note: assigned 2026-11-11; packing fixed 2026-11-12 | reserved-pre-zone |
| 5850-5899 | Core and messaging | saga-state-machine (#297 StateMachine 5850-5874, MessagingStateMachineSagas 5875-5899; rest of the family inside these) Note: assigned 2026-11-11 | reserved-pre-zone |
| 5900-5949 | Security and compliance | compliance-consent: ComplianceDSRExtensions (#1195 5900-5905, #2700 5906-5913) Note: assigned 2026-11-11 (was 9800-9849, which collided with NIS2) | reserved-pre-zone |
| 5950-5969 | Security and compliance | ComplianceAIActGPAI (#845 ids 5950-5957; no ids left in ComplianceAIAct; the assembly Encina.Compliance.AIAct maps to both ranges; name is misleading for core tenant logs, rename to ComplianceAIActTenancy proposed in the #845 plan) Note: assigned 2026-11-11; plan fixed and armed 2026-11-12 | reserved-pre-zone |
| 6000-6249 | Security and compliance | claim-check family: MessagingClaimCheck 6000-6049 (#599 6000-6024, #122 6025-6049), StorageCore 6050-6099 and StorageProviders 6100-6149 (#1239), ClaimCheckProviders 6150-6199 (#122), SecurityColumnEncryption 6200-6249 (#1250) Note: assigned 2026-11-11 (was 5500-5749, which collided with platform-spec002) | reserved-pre-zone |
| 6250-6299 | Caching | lock family: DistributedLock assembly (#741 6250-6269, #717 6270-6299) Note: assigned 2026-11-11 (was 5500-5549, collided with platform-spec002) | reserved-pre-zone |
| 6300-6349 | Core and messaging | lock family: Encina.Http (#1233) Note: assigned 2026-11-11 (was 5600-5649, collided with platform-spec002) | reserved-pre-zone |
| 6350-6359 | Security and compliance | ComplianceDPIALock (#763: 6350 skipped, 6351 lock-provider presence) Note: proposed in PR #3534 | reserved-pre-zone |
| 6360-6369 | Security and compliance | AuditMartenErasure (new range, Encina.Audit.Marten): #2847 recorder 6360-6364 Note: claimed 2026-11-12 (AuditMarten 2550-2599 is full) | reserved-pre-zone |
| 6370-6379 | Security and compliance | TenancyConfigurationAudit (new range): #752 ids 6370-6374 Note: claimed 2026-11-14; also #752 Security.Audit 5016-5018, #798 5013-5015 and 7400-7402, #1257 7420-7424 | reserved-pre-zone |
| 6400-6549 | Security and compliance | compliance-omnibus: ComplianceDPIACriteria 6400-6429 (#816), ComplianceENISA 6430-6449 (#1942), ComplianceEHDS 6450-6549 (#808) Note: assigned 2026-11-11 (was 9800-9959, collided with NIS2) | reserved-pre-zone |
| 6550-6599 | Security and compliance | wave 2: compliance-dsr-audit (family 13) Note: assigned 2026-11-11 | reserved-pre-zone |
| 6600-6649 | Security and compliance | wave 2: compliance-rules (14) Note: assigned | reserved-pre-zone |
| 6650-6699 | Core and messaging | wave 2: msg-dead-letter-ops (16) Note: assigned | reserved-pre-zone |
| 6700-6749 | Core and messaging | wave 2: msg-priority-partition (17) Note: assigned | reserved-pre-zone |
| 6750-6799 | Core and messaging | wave 2: scheduling-1 (18) Note: assigned | reserved-pre-zone |
| 6800-6849 | Core and messaging | wave 2: deadletter-retry-stores (19) Note: assigned | reserved-pre-zone |
| 6850-6899 | Observability and platform | wave 2: aspnet-api-3 (21) Note: assigned | reserved-pre-zone |
| 6900-6949 | Security and compliance | wave 2: audit-stores-2 (22) Note: assigned | reserved-pre-zone |
| 6950-6999 | Caching | wave 2: cache-providers-keys (23) Note: assigned | reserved-pre-zone |
| 7100-7149 | Core and messaging | wave 2: cdc-1 (24) Note: assigned | reserved-pre-zone |
| 7150-7199 | Observability and platform | wave 2: cli-1 (25) Note: assigned | reserved-pre-zone |
| 7200-7249 | Core and messaging | wave 2: es-integrity-crypto (27) Note: assigned | reserved-pre-zone |
| 7250-7299 | Core and messaging | wave 2: es-snapshots-projections (28) Note: assigned | reserved-pre-zone |
| 7300-7349 | Observability and platform | wave 2: feature-flags (29) Note: assigned | reserved-pre-zone |
| 7350-7399 | Observability and platform | wave 2: health-checks-1 (30) Note: assigned | reserved-pre-zone |
| 7400-7419 | Core and messaging | multitenancy-3: ABAC tenancy (#798) Note: assigned 2026-11-11 | reserved-pre-zone |
| 7420-7469 | Core and messaging | multitenancy-3: TenantIsolation (#1257) Note: assigned 2026-11-11 | reserved-pre-zone |
| 7500-7549 | Caching | wave 3: lock-messaging-integration (31): MessagingLocking 7500-7539 (#713 7500-7503, #714 7504, #715 7505, 7506-7539 free), MachineIdLease 7540-7549 (#2394 7540-7546) Note: assigned 2026-11-15; plans written locally (branches docs/plan-713/714/715/716/2394 in worktree pg-a, not pushed) | reserved-pre-zone |
| 7550-7599 | Caching | wave 3: lock-providers-1 (32): DistributedLockPostgreSQL 7550-7559 (#207 uses 7550-7557), DistributedLockMySQL 7560-7569, DistributedLockAzureBlob 7570-7579, DistributedLockDynamoDB 7580-7589, 7590-7599 spare; #609 MongoDB uses 3190-3198 inside the existing MongoDB range 3100-3199 (3199 spare; 3167-3189 left to the #594/#590 plans) Note: assigned 2026-11-15; plans written locally (pg-a) | reserved-pre-zone |
| 7600-7649 | Core and messaging | wave 3: modules-1 (34) Note: assigned 2026-11-15 | reserved-pre-zone |
| 7650-7699 | Core and messaging | wave 3: modules-2 (35) Note: assigned 2026-11-15 | reserved-pre-zone |
| 7700-7749 | Core and messaging | wave 3: modules-3 (36): #761 7700-7702, #2442 7710-7712, #753 7720-7725; #3445 uses Refit 4657-4659 Note: plans written 2026-11-17 | reserved-pre-zone |
| 7750-7799 | Core and messaging | wave 3: msg-batching (37): Batching 7750-7779 (#302 7750-7757, #349 7758-7765, #339 7766-7769, 7770-7779 spare); 7780-7799 free Note: plans written 2026-11-17 | reserved-pre-zone |
| 7800-7849 | Caching | wave 3: msg-idempotency-pipeline (38): CachingIdempotency 7800-7819 (#334 7800-7803), 7820-7829 unused, MessagingNotificationIdempotency 7830-7839 (#733 7830-7835), MessagingScheduledIdempotency 7840-7849 (#735 7840-7842) Note: plans written 2026-11-17 | reserved-pre-zone |
| 7850-7899 | Core and messaging | wave 3: multitenancy-1 (39): TenancyPipeline 7850-7879 (#346 7850-7852, #338 7853-7854, #304 7855-7856), MessagingTenancy 7880-7899 (#125 7880-7882) Note: plans written 2026-11-17 | reserved-pre-zone |
| 7900-7949 | Core and messaging | wave 3: multitenancy-2 (40) Note: assigned 2026-11-15 | reserved-pre-zone |
| 9700-9749 | Security and compliance | #812 incident reporting Note: existing claim (from the #812 plan) | reserved-pre-zone |
| 9750-9799 | Transports | wave 3 (plan-batch-2): transport-new-2 Note: assigned Conflict: 9750-9799 is registered as SecurityBreakTheGlass; the plan must take a block in the 10000+ zone. | superseded |
| 9800-9899 | Security and compliance | compliance-nis2-1 (#822 owns the range) Note: proposed in PR #3509 | reserved-pre-zone |
| 9900-9949 | Security and compliance | wave 3 (plan-batch-2): audit-stores-1 Note: assigned | reserved-pre-zone |

### Status vocabulary

| Status | Meaning |
| --- | --- |
| registered | An `EventIdRanges.cs` range with the same bounds exists. |
| reserved | A block in the 10000+ zone that a plan holds and code has not registered yet. |
| reserved-pre-zone | A block below 10000 recorded before the zone existed; the allow-list for the rule that nothing else may lie below 10000. |
| superseded | Kept for history and ignored by the checks. 9750-9799 conflicts with the registered `SecurityBreakTheGlass` range, so its plan must take a 10000+ block. |
| free | Ignored by the checks. |

Block 7900-7949 (multitenancy-2) is unused and therefore free again as of 2026-11-18; its row is kept until the migration (#4007) or a plan releases it.

## Ids inside existing package ranges

Plans claim single ids inside a range a package already owns. These claims are not blocks and are not parsed; check them before using an id.

| Range | Ids | Plan |
| --- | --- | --- |
| (existing ranges) | MessagingSaga 2206; Messaging 2971-2972 (verified free on main 2026-11-12) | #736 |
| AspNetCore 200-249 | 208-211 | #3443 |
| AspNetCore 200-249 | 212-217 | #710 |
| AspNetCore 200-249 | 218-225 | #2229 |
| Marten | 2701-2720 | #2383 (on main) |
| Marten | 2721-2730 | #2384 |
| Marten | 2731-2745 | #584 (if D2 keeps it in Encina.Marten) |
| Cdc 4800-4899 | 4854-4891 | #2393, #634, #635, #636 |
| Kafka | 4156-4175 | #587 |
| Security 8000-8009 | 8005-8007 | #204 |
| Tenancy | 1805-1810 | #711 |
| SecurityAuditRead | 1750-1755 | #1194 |
| AspNetCore 200-249 | 226-229 | #454 |
| AspNetCore 200-249 | 230-231 | #355 |
| ComplianceAIAct 9500-9599 | 9500-9512 core, 9513-9529 #847, 9530-9547 #842, 9548-9561 #838, 9562-9575 #840, 9576-9587 #841, 9588-9599 #843 (registered 2026-11-17 from the merged plans; #845 uses ComplianceAIActGPAI 5950-5969, not this range) | AI Act family (ADR 068) |
| Consent | #811 8269-8274, #1197 8275-8276, #810 8277-8280, #1199 8281-8289, #1255 8290-8295; 8296-8299 free | 2026-11-11 |
| ProcessorAgreements | 9486-9493 #1198 | |
| SecuritySecrets 5100-5199 | 5151-5170 | #1246 (tenant-aware secrets) |
| Marten | 2746 | #1118 |
| MartenGDPRCryptoShredding 8450-8499 | 8485-8488 | #2455 (merged plan) |
| MartenGDPRCryptoShredding 8450-8499 | 8489-8491 | #2457 (8491 only if the UserId warning is chosen) |
| AuditMarten 2550-2599 | 2593-2598 | #3049 (merged plan); 2599 free |
| AuditMartenSubjectErasure (new, inside the 7200-7249 es-integrity-crypto block) | range 7200-7224, ids 7200-7207 used | #2903 |
| SecurityAuditRead 1750-1755 | only 1750-1751 used | #1194 |
| (new range) ComplianceNIS2Governance 7470-7499 | #827 (NIS2 sibling of #822); the #827 plan registers the range and uses ids from 7470 up | assigned 2026-11-13 |
| DPIA | 8792-8794 #813, 8897-8899 #1243, 8849 free | |
| LawfulBasis | 8387-8390 #815 | |
| MessagingScheduling 2300-2399 | 2326-2389 (#1222, #146, #150, #133, #147) | |
| FeatureFlags (new, #604) | 7300-7349: #604 owns the range; #300 7306-7309, #345 7310-7315, #451 7330-7339, #607 ServiceDiscovery 7340-7349 | |
| Family blocks used | 6900-6949 (#582 6900-6914, #585 6925-6934, #586 6935-6949); 6950-6979 #277; 7100-7119 #633; 7250-7299 es-snapshots (#320, #323, #324, #326, #535); 6650-6689 (#135, #2013); 6550-6599 (#1248, #1242, #1240, #1241) | |

### msg-batching claims inside existing ranges (2026-11-17)

| Range | Ids | Plan |
| --- | --- | --- |
| Messaging | 2973-2976 | #121 |
| Kafka | 4176-4177 | #252 |
| AzureServiceBus | 4312-4314 | #252 |
| RabbitMQ | 4106-4108 | #252 |
| NATS | 4210-4212 | #252 |
| MQTT | 4256-4258 | #252 |
| RedisPubSub | 4406-4408 | #252 |
| InMemory | 4460-4462 | #252 |
| SQS | 4356-4359 (reused) | #252 |

### multitenancy-2 claims inside existing ranges (2026-11-18)

| Range | Ids | Plan |
| --- | --- | --- |
| Messaging outbox | 2007-2009 | #737 |
| Messaging inbox | 2113-2115 | #760 |
| Polly | 3922-3924 | #740 |

### observability-core-1 / misc-startup claims inside existing ranges (2026-11-18)

| Range | Ids | Plan |
| --- | --- | --- |
| Core | 177-183 | #2382 |
| Core | 184-185 | #132 |
| Messaging | 2977-2979 (reserved if planned) | #176 |

## ADR numbers

Plans name an ADR number only from this table; otherwise they write "ADR number assigned when the ADR pull request opens". Existing: 063 is the last merged; 059 and 061 are reserved in [the ADR index](../architecture/adr/index.md).

| ADR | Owner |
| --- | --- |
| 062 | #3419 error category contract (sub-issue S1) |
| 064 | NIS2 family (#822 plan) |
| 065 | claim check (#122 plan) |
| 066 | column encryption (#1250 plan) |
| 067 | Encina.Storage (#1239 plan) |
| 068 | AI Act family (#838 plan) |

## History: the exhaustion decision (2026-11-18)

On 2026-11-18 the maintainer chose option D for the exhaustion of the 1-9999 space: a 10000+ zone with one base per area. This decision is superseded by the "Amendment (2026-11-20, #4006): the 10000+ zone" of [ADR-021](../architecture/adr/021-eventid-uniqueness-enforcement.md), which holds the zone rule. Blocks already reserved below 10000 are not rewritten; their migration is issue #4007.
