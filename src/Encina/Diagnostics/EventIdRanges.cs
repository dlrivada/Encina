using System.Reflection;

namespace Encina.Diagnostics;

/// <summary>
/// Centralized EventId range allocation for all Encina packages.
/// Each package MUST register its range here before using EventIds.
/// Validated at test time by architecture tests in <c>Encina.Testing.Architecture</c>.
/// </summary>
/// <remarks>
/// <para>
/// Allocation policy:
/// <list type="bullet">
/// <item>Each package gets one or more contiguous ranges; no two ranges overlap</item>
/// <item>Ranges are grouped by functional area (core, messaging, security, compliance)</item>
/// <item>New packages must register a free range via PR before using EventIds</item>
/// <item>Use <see cref="GetAllRanges"/> to discover all registered allocations</item>
/// </list>
/// </para>
/// <para>
/// Migration into the 10000+ zone (issue #4007): the 91 ranges below are the historical ranges. They are grouped
/// by the area and sub-issue that moves them, in the merge order of #4007 (CM-1 to CM-6, DB-1, DB-2, CA-1, CA-2,
/// TR-1, TR-2, SC-1 to SC-6, OP-1, OP-2, TT-CI). Each field keeps its current value until its sub-issue lands;
/// the trailing comment names the sub-issue and the range the field moves to. The target extents are reserved in
/// <c>docs/engineering/eventid-block-registry.md</c> (see its "Migration map"). New ranges are born in the
/// 10000+ zone and are not listed here by area banner.
/// </para>
/// </remarks>
public static class EventIdRanges
{
    // ═══════════════════════════════════════════════════════════════════════
    // Core and messaging, target extent 10000-12199 (#4007 CM-1 to CM-6)
    // ═══════════════════════════════════════════════════════════════════════

    // ── CM-1 Core ──────────────────────────────────────────────────────────

    /// <summary>Encina — mediator dispatch, streaming and sharding (module isolation has its own range).</summary>
    public static readonly (int Min, int Max) Core = (100, 199); // moves to 10000-10099 in #4007 CM-1

    // ── CM-2 DomainModeling and Tenancy ────────────────────────────────────

    /// <summary>Encina.DomainModeling — repository operations.</summary>
    public static readonly (int Min, int Max) DomainModelingRepository = (1100, 1199); // moves to 10100-10199 in #4007 CM-2

    /// <summary>Encina.DomainModeling — unit of work / transaction operations.</summary>
    public static readonly (int Min, int Max) DomainModelingUnitOfWork = (1200, 1299); // moves to 10200-10299 in #4007 CM-2

    /// <summary>Encina.DomainModeling — bulk operations.</summary>
    public static readonly (int Min, int Max) DomainModelingBulkOperations = (1300, 1399); // moves to 10300-10399 in #4007 CM-2

    /// <summary>Encina.DomainModeling — specification evaluation.</summary>
    public static readonly (int Min, int Max) DomainModelingSpecification = (1400, 1499); // moves to 10400-10499 in #4007 CM-2

    /// <summary>Encina.DomainModeling — audit trail operations.</summary>
    public static readonly (int Min, int Max) DomainModelingAudit = (1600, 1699); // moves to 10500-10599 in #4007 CM-2

    /// <summary>Encina.Tenancy — multi-tenancy operations.</summary>
    public static readonly (int Min, int Max) Tenancy = (1800, 1899); // moves to 10600-10699 in #4007 CM-2

    /// <summary>Encina.DomainModeling — domain event operations.</summary>
    public static readonly (int Min, int Max) DomainModelingDomainEvents = (2500, 2549); // moves to 11250-11299 in #4007 CM-2

    /// <summary>Encina.Tenancy.AspNetCore — tenant resolution middleware and resolvers.</summary>
    public static readonly (int Min, int Max) TenancyAspNetCore = (3850, 3899); // moves to 11750-11799 in #4007 CM-2

    // ── CM-3 Module isolation, messaging stores and IdGeneration ───────────

    /// <summary>Encina — module isolation operations.</summary>
    public static readonly (int Min, int Max) ModuleIsolation = (1900, 1999); // moves to 10700-10799 in #4007 CM-3

    /// <summary>Encina.Messaging — outbox store operations.</summary>
    public static readonly (int Min, int Max) MessagingOutbox = (2000, 2099); // moves to 10800-10899 in #4007 CM-3

    /// <summary>Encina.Messaging — inbox store operations.</summary>
    public static readonly (int Min, int Max) MessagingInbox = (2100, 2199); // moves to 10900-10999 in #4007 CM-3

    /// <summary>Encina.Messaging — saga store operations.</summary>
    public static readonly (int Min, int Max) MessagingSaga = (2200, 2299); // moves to 11000-11099 in #4007 CM-3

    /// <summary>Encina.Messaging — scheduled message store operations.</summary>
    public static readonly (int Min, int Max) MessagingScheduling = (2300, 2399); // moves to 11100-11199 in #4007 CM-3

    /// <summary>Encina.IdGeneration — distributed ID generation.</summary>
    public static readonly (int Min, int Max) IdGeneration = (8030, 8099); // moves to 12100-12199 in #4007 CM-3

    // ── CM-4 Messaging core and recoverability ─────────────────────────────

    /// <summary>Encina.Messaging — orchestrators and runners (the stores have their own ranges above).</summary>
    public static readonly (int Min, int Max) Messaging = (2800, 2999); // moves to 11550-11749 in #4007 CM-4

    /// <summary>
    /// Encina.Messaging — delayed-retry processor and recoverability pipeline diagnostics added after
    /// the original recoverability ids (2845-2871, inside <see cref="Messaging"/>).
    /// </summary>
    public static readonly (int Min, int Max) MessagingRecoverability = (5450, 5499); // moves to 12050-12099 in #4007 CM-4

    // ── CM-5 Messaging encryption and change data capture ──────────────────

    /// <summary>Encina.Messaging.Encryption — message encryption/decryption.</summary>
    public static readonly (int Min, int Max) MessagingEncryption = (2450, 2499); // moves to 11200-11249 in #4007 CM-5

    /// <summary>Encina.Cdc — CDC processor, sharded connectors, messaging bridge and cache invalidation.</summary>
    public static readonly (int Min, int Max) Cdc = (4800, 4899); // moves to 11800-11899 in #4007 CM-5

    /// <summary>Encina.Cdc.SqlServer — SQL Server CDC connector.</summary>
    public static readonly (int Min, int Max) CdcSqlServer = (4900, 4949); // moves to 11900-11949 in #4007 CM-5

    /// <summary>Encina.Cdc.Debezium — Debezium connector.</summary>
    public static readonly (int Min, int Max) CdcDebezium = (4950, 4999); // moves to 11950-11999 in #4007 CM-5

    /// <summary>Encina.Cdc.MySql — MySQL binary log CDC connector.</summary>
    public static readonly (int Min, int Max) CdcMySql = (5400, 5449); // moves to 12000-12049 in #4007 CM-5

    // ── CM-6 Event sourcing (Marten) ───────────────────────────────────────

    /// <summary>Encina.Audit.Marten — event-sourced audit store.</summary>
    public static readonly (int Min, int Max) AuditMarten = (2550, 2599); // moves to 11300-11349 in #4007 CM-6

    /// <summary>
    /// Encina.Marten — aggregate repositories (2600-2616), event versioning (2617-2628),
    /// snapshots (2629-2649), event metadata (2650-2663) and projections (2664-2700);
    /// 2701-2799 free for the package.
    /// </summary>
    public static readonly (int Min, int Max) Marten = (2600, 2799); // moves to 11350-11549 in #4007 CM-6

    // ═══════════════════════════════════════════════════════════════════════
    // Database, target extent 20000-20649 (#4007 DB-1, DB-2)
    // ═══════════════════════════════════════════════════════════════════════

    // ── DB-1 Entity Framework Core ─────────────────────────────────────────

    /// <summary>Encina.EntityFrameworkCore — soft delete operations.</summary>
    public static readonly (int Min, int Max) EntityFrameworkCoreSoftDelete = (1500, 1599); // moves to 20000-20099 in #4007 DB-1

    /// <summary>Encina.EntityFrameworkCore — query cache operations.</summary>
    public static readonly (int Min, int Max) EntityFrameworkCoreQueryCache = (2400, 2449); // moves to 20100-20149 in #4007 DB-1

    /// <summary>Encina.EntityFrameworkCore — transactions, outbox processing, auditing, temporal, read/write routing.</summary>
    public static readonly (int Min, int Max) EntityFrameworkCore = (3000, 3099); // moves to 20150-20249 in #4007 DB-1

    // ── DB-2 MongoDB, ADO.NET and Dapper ───────────────────────────────────

    /// <summary>Encina.MongoDB — messaging stores, auditing, module databases, read/write routing.</summary>
    public static readonly (int Min, int Max) MongoDB = (3100, 3199); // moves to 20250-20349 in #4007 DB-2

    /// <summary>Encina.ADO.SqlServer — temporal repository, read/write routing.</summary>
    public static readonly (int Min, int Max) ADOSqlServer = (3200, 3249); // moves to 20350-20399 in #4007 DB-2

    /// <summary>Encina.ADO.PostgreSQL — temporal repository, read/write routing.</summary>
    public static readonly (int Min, int Max) ADOPostgreSQL = (3250, 3299); // moves to 20400-20449 in #4007 DB-2

    /// <summary>Encina.ADO.MySQL — read/write routing.</summary>
    public static readonly (int Min, int Max) ADOMySQL = (3300, 3349); // moves to 20450-20499 in #4007 DB-2

    /// <summary>Encina.Dapper.SqlServer — temporal repository, read/write routing.</summary>
    public static readonly (int Min, int Max) DapperSqlServer = (3350, 3399); // moves to 20500-20549 in #4007 DB-2

    /// <summary>Encina.Dapper.PostgreSQL — temporal repository, read/write routing.</summary>
    public static readonly (int Min, int Max) DapperPostgreSQL = (3400, 3449); // moves to 20550-20599 in #4007 DB-2

    /// <summary>Encina.Dapper.MySQL — read/write routing.</summary>
    public static readonly (int Min, int Max) DapperMySQL = (3450, 3499); // moves to 20600-20649 in #4007 DB-2

    // ═══════════════════════════════════════════════════════════════════════
    // Caching and distributed locks, target extent 30000-30349 (#4007 CA-1, CA-2)
    // ═══════════════════════════════════════════════════════════════════════

    // ── CA-1 Caching ───────────────────────────────────────────────────────

    /// <summary>Encina.Caching — query caching, invalidation and idempotency behaviors.</summary>
    public static readonly (int Min, int Max) Caching = (3500, 3549); // moves to 30000-30049 in #4007 CA-1

    /// <summary>Encina.Caching.Memory — in-memory cache, lock and pub/sub providers.</summary>
    public static readonly (int Min, int Max) CachingMemory = (3550, 3599); // moves to 30050-30099 in #4007 CA-1

    /// <summary>Encina.Caching.Redis — Redis cache, lock and pub/sub providers.</summary>
    public static readonly (int Min, int Max) CachingRedis = (3600, 3649); // moves to 30100-30149 in #4007 CA-1

    /// <summary>Encina.Caching.Hybrid — HybridCache provider.</summary>
    public static readonly (int Min, int Max) CachingHybrid = (3650, 3699); // moves to 30150-30199 in #4007 CA-1

    // ── CA-2 Distributed locks ─────────────────────────────────────────────

    /// <summary>Encina.DistributedLock.InMemory — in-process lock provider.</summary>
    public static readonly (int Min, int Max) DistributedLockInMemory = (3700, 3749); // moves to 30200-30249 in #4007 CA-2

    /// <summary>Encina.DistributedLock.Redis — Redis lock provider.</summary>
    public static readonly (int Min, int Max) DistributedLockRedis = (3750, 3799); // moves to 30250-30299 in #4007 CA-2

    /// <summary>Encina.DistributedLock.SqlServer — SQL Server lock provider.</summary>
    public static readonly (int Min, int Max) DistributedLockSqlServer = (3800, 3849); // moves to 30300-30349 in #4007 CA-2

    // ═══════════════════════════════════════════════════════════════════════
    // Transports, target extent 40000-40549 (#4007 TR-1, TR-2)
    // ═══════════════════════════════════════════════════════════════════════

    // ── TR-1 Message brokers and cloud queues ──────────────────────────────

    /// <summary>Encina.RabbitMQ — message transport.</summary>
    public static readonly (int Min, int Max) RabbitMQ = (4100, 4149); // moves to 40000-40049 in #4007 TR-1

    /// <summary>Encina.Kafka — message transport.</summary>
    public static readonly (int Min, int Max) Kafka = (4150, 4199); // moves to 40050-40099 in #4007 TR-1

    /// <summary>Encina.NATS — message transport.</summary>
    public static readonly (int Min, int Max) NATS = (4200, 4249); // moves to 40100-40149 in #4007 TR-1

    /// <summary>Encina.MQTT — message transport.</summary>
    public static readonly (int Min, int Max) MQTT = (4250, 4299); // moves to 40150-40199 in #4007 TR-1

    /// <summary>Encina.AzureServiceBus — message transport.</summary>
    public static readonly (int Min, int Max) AzureServiceBus = (4300, 4349); // moves to 40200-40249 in #4007 TR-1

    /// <summary>Encina.AmazonSQS — message transport.</summary>
    public static readonly (int Min, int Max) AmazonSQS = (4350, 4399); // moves to 40250-40299 in #4007 TR-1

    // ── TR-2 Pub/sub, in-process and API transports ────────────────────────

    /// <summary>Encina.Redis.PubSub — message transport.</summary>
    public static readonly (int Min, int Max) RedisPubSub = (4400, 4449); // moves to 40300-40349 in #4007 TR-2

    /// <summary>Encina.InMemory — in-memory message transport.</summary>
    public static readonly (int Min, int Max) InMemory = (4450, 4499); // moves to 40350-40399 in #4007 TR-2

    /// <summary>Encina.gRPC — request/response transport.</summary>
    public static readonly (int Min, int Max) GRpc = (4500, 4549); // moves to 40400-40449 in #4007 TR-2

    /// <summary>Encina.GraphQL — query and mutation bridge.</summary>
    public static readonly (int Min, int Max) GraphQL = (4550, 4599); // moves to 40450-40499 in #4007 TR-2

    /// <summary>Encina.SignalR — real-time notification broadcasting.</summary>
    public static readonly (int Min, int Max) SignalR = (4600, 4649); // moves to 40500-40549 in #4007 TR-2

    // ═══════════════════════════════════════════════════════════════════════
    // Security and compliance, target extent 50000-52349 (#4007 SC-1 to SC-6)
    // ═══════════════════════════════════════════════════════════════════════

    // ── SC-1 Sanitization, security core, PII and audit ────────────────────

    /// <summary>Encina.Security.Sanitization — input sanitization pipeline.</summary>
    public static readonly (int Min, int Max) Sanitization = (1, 99); // moves to 50000-50099 in #4007 SC-1

    /// <summary>Encina.Security.Audit — read audit operations.</summary>
    public static readonly (int Min, int Max) SecurityAuditRead = (1700, 1799); // moves to 50100-50199 in #4007 SC-1

    /// <summary>Encina.Security.Audit — audit pipeline and retention (read audit has <see cref="SecurityAuditRead"/>).</summary>
    public static readonly (int Min, int Max) SecurityAudit = (5000, 5099); // moves to 50200-50299 in #4007 SC-1

    /// <summary>Encina.Security — core authorization pipeline.</summary>
    public static readonly (int Min, int Max) Security = (8000, 8009); // moves to 50600-50649 in #4007 SC-1

    /// <summary>Encina.Security.PII — PII masking operations.</summary>
    public static readonly (int Min, int Max) SecurityPII = (8010, 8029); // moves to 50650-50699 in #4007 SC-1

    // ── SC-2 Secrets ───────────────────────────────────────────────────────

    /// <summary>Encina.Security.Secrets — secret readers, injection, rotation, caching, failover and health checks.</summary>
    public static readonly (int Min, int Max) SecuritySecrets = (5100, 5199); // moves to 50300-50399 in #4007 SC-2

    /// <summary>Encina.Security.Secrets.AzureKeyVault — Azure Key Vault secret provider.</summary>
    public static readonly (int Min, int Max) SecuritySecretsAzureKeyVault = (5200, 5249); // moves to 50400-50449 in #4007 SC-2

    /// <summary>Encina.Security.Secrets.AwsSecretsManager — AWS Secrets Manager provider.</summary>
    public static readonly (int Min, int Max) SecuritySecretsAwsSecretsManager = (5250, 5299); // moves to 50450-50499 in #4007 SC-2

    /// <summary>Encina.Security.Secrets.HashiCorpVault — HashiCorp Vault provider.</summary>
    public static readonly (int Min, int Max) SecuritySecretsHashiCorpVault = (5300, 5349); // moves to 50500-50549 in #4007 SC-2

    /// <summary>Encina.Security.Secrets.GoogleCloudSecretManager — Google Cloud Secret Manager provider.</summary>
    public static readonly (int Min, int Max) SecuritySecretsGoogleCloudSecretManager = (5350, 5399); // moves to 50550-50599 in #4007 SC-2

    // ── SC-3 ABAC, anti-tampering and break-the-glass ──────────────────────

    /// <summary>
    /// Encina.Security.ABAC — attribute-based access control.
    /// The ABAC [LoggerMessage] ids are allocated inside 9000-9099 (see ABACLogMessages.cs, PersistentPolicyAdministrationPoint.cs and ABACPolicySeedingHostedService.cs).
    /// Unused ids to reuse first: 9007, 9016-9019, 9023-9029, 9041-9049, 9056-9057, 9090 (freed by #3271).
    /// Used: 9006 by the break-the-glass gate integration of #1244 (ABAC enforcement overridden by an emergency elevation).
    /// Used: 9079-9089 by the decision audit of #751 (9084, 9086 and 9087 are its startup check).
    /// </summary>
    public static readonly (int Min, int Max) SecurityABAC = (9000, 9099); // moves to 51550-51649 in #4007 SC-3

    /// <summary>Encina.Security.AntiTampering — signature validation.</summary>
    public static readonly (int Min, int Max) SecurityAntiTampering = (9100, 9199); // moves to 51650-51749 in #4007 SC-3

    /// <summary>Encina.Security.BreakTheGlass — break-the-glass emergency access (gate, sweeper, health, start-up).</summary>
    public static readonly (int Min, int Max) SecurityBreakTheGlass = (9750, 9799); // moves to 52300-52349 in #4007 SC-3

    // ── SC-4 GDPR, consent, data subject rights, lawful basis, anonymization ──

    /// <summary>Encina.Compliance.GDPR — GDPR compliance pipeline.</summary>
    public static readonly (int Min, int Max) ComplianceGDPR = (8100, 8199); // moves to 50700-50799 in #4007 SC-4

    /// <summary>Encina.Compliance.Consent — consent management.</summary>
    public static readonly (int Min, int Max) ComplianceConsent = (8200, 8299); // moves to 50800-50899 in #4007 SC-4

    /// <summary>Encina.Compliance.DataSubjectRights — data subject rights (Art. 15-22).</summary>
    public static readonly (int Min, int Max) ComplianceDSR = (8300, 8349); // moves to 50900-50949 in #4007 SC-4

    /// <summary>Encina.Compliance.LawfulBasis — lawful basis validation (Art. 6).</summary>
    public static readonly (int Min, int Max) ComplianceLawfulBasis = (8350, 8399); // moves to 50950-50999 in #4007 SC-4

    /// <summary>Encina.Compliance.Anonymization — data anonymization.</summary>
    public static readonly (int Min, int Max) ComplianceAnonymization = (8400, 8449); // moves to 51000-51049 in #4007 SC-4

    /// <summary>Encina.Marten.GDPR — crypto-shredding for GDPR.</summary>
    public static readonly (int Min, int Max) MartenGDPRCryptoShredding = (8450, 8499); // moves to 51050-51099 in #4007 SC-4

    // ── SC-5 Retention, data residency, breach notification, DPIA, privacy by design ──

    /// <summary>Encina.Compliance.Retention — data retention enforcement.</summary>
    public static readonly (int Min, int Max) ComplianceRetention = (8500, 8599); // moves to 51100-51199 in #4007 SC-5

    /// <summary>Encina.Compliance.DataResidency — data residency compliance.</summary>
    public static readonly (int Min, int Max) ComplianceDataResidency = (8600, 8699); // moves to 51200-51299 in #4007 SC-5

    /// <summary>Encina.Compliance.BreachNotification — breach notification (Art. 33-34).</summary>
    public static readonly (int Min, int Max) ComplianceBreachNotification = (8700, 8799); // moves to 51300-51399 in #4007 SC-5

    /// <summary>Encina.Compliance.DPIA — Data Protection Impact Assessment (Art. 35).</summary>
    public static readonly (int Min, int Max) ComplianceDPIA = (8800, 8899); // moves to 51400-51499 in #4007 SC-5

    /// <summary>Encina.Compliance.PrivacyByDesign — privacy by design (Art. 25).</summary>
    public static readonly (int Min, int Max) CompliancePrivacyByDesign = (8900, 8949); // moves to 51500-51549 in #4007 SC-5

    // ── SC-6 NIS2, cross-border transfer, processor agreements, AI Act, attestation ──

    /// <summary>Encina.Compliance.NIS2 — NIS2 Directive cybersecurity compliance.</summary>
    public static readonly (int Min, int Max) ComplianceNIS2 = (9200, 9299); // moves to 51750-51849 in #4007 SC-6

    /// <summary>Encina.Compliance.CrossBorderTransfer — cross-border transfer (Ch. V).</summary>
    public static readonly (int Min, int Max) ComplianceCrossBorderTransfer = (9300, 9399); // moves to 51850-51949 in #4007 SC-6

    /// <summary>Encina.Compliance.ProcessorAgreements — processor agreement management (Art. 28).</summary>
    public static readonly (int Min, int Max) ComplianceProcessorAgreements = (9400, 9499); // moves to 52000-52099 in #4007 SC-6 (starts at 52000 so it does not straddle the 51999 boundary)

    /// <summary>Encina.Compliance.AIAct — EU AI Act compliance pipeline.</summary>
    public static readonly (int Min, int Max) ComplianceAIAct = (9500, 9599); // moves to 52100-52199 in #4007 SC-6

    /// <summary>Encina.Compliance.Attestation — tamper-evident audit attestation.</summary>
    public static readonly (int Min, int Max) ComplianceAttestation = (9600, 9699); // moves to 52200-52299 in #4007 SC-6

    // ═══════════════════════════════════════════════════════════════════════
    // Observability and platform, target extent 60000-60349 (#4007 OP-1, OP-2)
    // ═══════════════════════════════════════════════════════════════════════

    // ── OP-1 ASP.NET Core and OpenTelemetry ────────────────────────────────

    /// <summary>Encina.AspNetCore — authorization pipeline behavior.</summary>
    public static readonly (int Min, int Max) AspNetCore = (200, 249); // moves to 60000-60049 in #4007 OP-1

    /// <summary>Encina.OpenTelemetry — resharding and observability log messages.</summary>
    public static readonly (int Min, int Max) OpenTelemetry = (7000, 7099); // moves to 60250-60349 in #4007 OP-1

    // ── OP-2 Resilience and scheduling adapters ────────────────────────────

    /// <summary>Encina.Polly — retry, circuit breaker, bulkhead and rate limiting behaviors.</summary>
    public static readonly (int Min, int Max) Polly = (3900, 3949); // moves to 60050-60099 in #4007 OP-2

    /// <summary>Encina.Extensions.Resilience — standard resilience pipeline behavior.</summary>
    public static readonly (int Min, int Max) ExtensionsResilience = (3950, 3999); // moves to 60100-60149 in #4007 OP-2

    /// <summary>Encina.Hangfire — request and notification jobs.</summary>
    public static readonly (int Min, int Max) Hangfire = (4000, 4049); // moves to 60150-60199 in #4007 OP-2

    /// <summary>Encina.Quartz — request and notification jobs.</summary>
    public static readonly (int Min, int Max) Quartz = (4050, 4099); // moves to 60200-60249 in #4007 OP-2

    // ═══════════════════════════════════════════════════════════════════════
    // Testing and tooling, target extent 70000-70049 (#4007 TT-CI)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Testing — module architecture analyzer diagnostics.</summary>
    public static readonly (int Min, int Max) Testing = (250, 299); // moves to 70000-70049 in #4007 TT-CI

    // ═══════════════════════════════════════════════════════════════════════
    // Cloud and integration, target extent 80000-80149 (#4007 TT-CI)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Refit — REST API request handler.</summary>
    public static readonly (int Min, int Max) Refit = (4650, 4699); // moves to 80000-80049 in #4007 TT-CI

    /// <summary>Encina.AwsLambda — Lambda execution and triggers.</summary>
    public static readonly (int Min, int Max) AwsLambda = (4700, 4749); // moves to 80050-80099 in #4007 TT-CI

    /// <summary>Encina.AzureFunctions — Functions execution and triggers.</summary>
    public static readonly (int Min, int Max) AzureFunctions = (4750, 4799); // moves to 80100-80149 in #4007 TT-CI

    /// <summary>
    /// Returns all registered ranges for validation and diagnostics.
    /// </summary>
    /// <returns>A list of tuples containing range name, min, and max EventId.</returns>
    public static IReadOnlyList<(string Name, int Min, int Max)> GetAllRanges()
    {
        return typeof(EventIdRanges)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof((int, int)))
            .Select(f =>
            {
                var range = ((int Min, int Max))f.GetValue(null)!;
                return (Name: f.Name, Min: range.Min, Max: range.Max);
            })
            .OrderBy(r => r.Min)
            .ToList()
            .AsReadOnly();
    }
}
