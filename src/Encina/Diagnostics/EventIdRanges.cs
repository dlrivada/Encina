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
/// <item>Each package gets a contiguous, non-overlapping range</item>
/// <item>Ranges are grouped by functional area (core, messaging, security, compliance)</item>
/// <item>New packages must register a free range via PR before using EventIds</item>
/// <item>Use <see cref="GetAllRanges"/> to discover all registered allocations</item>
/// </list>
/// </para>
/// </remarks>
public static class EventIdRanges
{
    // ═══════════════════════════════════════════════════════════════════════
    // Core packages (1-199)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Security.Sanitization — input sanitization pipeline.</summary>
    public static readonly (int Min, int Max) Sanitization = (1, 99);

    /// <summary>Encina — mediator dispatch, streaming and sharding (module isolation has its own range).</summary>
    public static readonly (int Min, int Max) Core = (100, 199);

    // ═══════════════════════════════════════════════════════════════════════
    // DomainModeling (1100-1699)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.DomainModeling — repository operations.</summary>
    public static readonly (int Min, int Max) DomainModelingRepository = (1100, 1199);

    /// <summary>Encina.DomainModeling — unit of work / transaction operations.</summary>
    public static readonly (int Min, int Max) DomainModelingUnitOfWork = (1200, 1299);

    /// <summary>Encina.DomainModeling — bulk operations.</summary>
    public static readonly (int Min, int Max) DomainModelingBulkOperations = (1300, 1399);

    /// <summary>Encina.DomainModeling — specification evaluation.</summary>
    public static readonly (int Min, int Max) DomainModelingSpecification = (1400, 1499);

    /// <summary>Encina.EntityFrameworkCore — soft delete operations.</summary>
    public static readonly (int Min, int Max) EntityFrameworkCoreSoftDelete = (1500, 1599);

    /// <summary>Encina.DomainModeling — audit trail operations.</summary>
    public static readonly (int Min, int Max) DomainModelingAudit = (1600, 1699);

    // ═══════════════════════════════════════════════════════════════════════
    // Security Audit (1700-1799)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Security.Audit — read audit operations.</summary>
    public static readonly (int Min, int Max) SecurityAuditRead = (1700, 1799);

    // ═══════════════════════════════════════════════════════════════════════
    // Infrastructure (1800-1999)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Tenancy — multi-tenancy operations.</summary>
    public static readonly (int Min, int Max) Tenancy = (1800, 1899);

    /// <summary>Encina — module isolation operations.</summary>
    public static readonly (int Min, int Max) ModuleIsolation = (1900, 1999);

    // ═══════════════════════════════════════════════════════════════════════
    // Messaging (2000-2499)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Messaging — outbox store operations.</summary>
    public static readonly (int Min, int Max) MessagingOutbox = (2000, 2099);

    /// <summary>Encina.Messaging — inbox store operations.</summary>
    public static readonly (int Min, int Max) MessagingInbox = (2100, 2199);

    /// <summary>Encina.Messaging — saga store operations.</summary>
    public static readonly (int Min, int Max) MessagingSaga = (2200, 2299);

    /// <summary>Encina.Messaging — scheduled message store operations.</summary>
    public static readonly (int Min, int Max) MessagingScheduling = (2300, 2399);

    /// <summary>Encina.EntityFrameworkCore — query cache operations.</summary>
    public static readonly (int Min, int Max) EntityFrameworkCoreQueryCache = (2400, 2449);

    /// <summary>Encina.Messaging.Encryption — message encryption/decryption.</summary>
    public static readonly (int Min, int Max) MessagingEncryption = (2450, 2499);

    // ═══════════════════════════════════════════════════════════════════════
    // Domain Events & Event Sourcing (2500-2799)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.DomainModeling — domain event operations.</summary>
    public static readonly (int Min, int Max) DomainModelingDomainEvents = (2500, 2549);

    /// <summary>Encina.Audit.Marten — event-sourced audit store.</summary>
    public static readonly (int Min, int Max) AuditMarten = (2550, 2599);

    /// <summary>
    /// Encina.Marten — aggregate repositories (2600-2616), event versioning (2617-2628),
    /// snapshots (2629-2649), event metadata (2650-2663) and projections (2664-2700);
    /// 2701-2799 free for the package.
    /// </summary>
    public static readonly (int Min, int Max) Marten = (2600, 2799);

    // ═══════════════════════════════════════════════════════════════════════
    // Messaging runtime and data access providers (2800-3499)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Messaging — orchestrators and runners (the stores have their own ranges above).</summary>
    public static readonly (int Min, int Max) Messaging = (2800, 2999);

    /// <summary>Encina.EntityFrameworkCore — transactions, outbox processing, auditing, temporal, read/write routing.</summary>
    public static readonly (int Min, int Max) EntityFrameworkCore = (3000, 3099);

    /// <summary>Encina.MongoDB — messaging stores, auditing, module databases, read/write routing.</summary>
    public static readonly (int Min, int Max) MongoDB = (3100, 3199);

    /// <summary>Encina.ADO.SqlServer — temporal repository, read/write routing.</summary>
    public static readonly (int Min, int Max) ADOSqlServer = (3200, 3249);

    /// <summary>Encina.ADO.PostgreSQL — temporal repository, read/write routing.</summary>
    public static readonly (int Min, int Max) ADOPostgreSQL = (3250, 3299);

    /// <summary>Encina.ADO.MySQL — read/write routing.</summary>
    public static readonly (int Min, int Max) ADOMySQL = (3300, 3349);

    /// <summary>Encina.Dapper.SqlServer — temporal repository, read/write routing.</summary>
    public static readonly (int Min, int Max) DapperSqlServer = (3350, 3399);

    /// <summary>Encina.Dapper.PostgreSQL — temporal repository, read/write routing.</summary>
    public static readonly (int Min, int Max) DapperPostgreSQL = (3400, 3449);

    /// <summary>Encina.Dapper.MySQL — read/write routing.</summary>
    public static readonly (int Min, int Max) DapperMySQL = (3450, 3499);

    // ═══════════════════════════════════════════════════════════════════════
    // Caching and distributed locks (3500-3899)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Caching — query caching, invalidation and idempotency behaviors.</summary>
    public static readonly (int Min, int Max) Caching = (3500, 3549);

    /// <summary>Encina.Caching.Memory — in-memory cache, lock and pub/sub providers.</summary>
    public static readonly (int Min, int Max) CachingMemory = (3550, 3599);

    /// <summary>Encina.Caching.Redis — Redis cache, lock and pub/sub providers.</summary>
    public static readonly (int Min, int Max) CachingRedis = (3600, 3649);

    /// <summary>Encina.Caching.Hybrid — HybridCache provider.</summary>
    public static readonly (int Min, int Max) CachingHybrid = (3650, 3699);

    /// <summary>Encina.DistributedLock.InMemory — in-process lock provider.</summary>
    public static readonly (int Min, int Max) DistributedLockInMemory = (3700, 3749);

    /// <summary>Encina.DistributedLock.Redis — Redis lock provider.</summary>
    public static readonly (int Min, int Max) DistributedLockRedis = (3750, 3799);

    /// <summary>Encina.DistributedLock.SqlServer — SQL Server lock provider.</summary>
    public static readonly (int Min, int Max) DistributedLockSqlServer = (3800, 3849);

    // ═══════════════════════════════════════════════════════════════════════
    // Resilience and scheduling adapters (3900-4099)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Polly — retry, circuit breaker, bulkhead and rate limiting behaviors.</summary>
    public static readonly (int Min, int Max) Polly = (3900, 3949);

    /// <summary>Encina.Extensions.Resilience — standard resilience pipeline behavior.</summary>
    public static readonly (int Min, int Max) ExtensionsResilience = (3950, 3999);

    /// <summary>Encina.Hangfire — request and notification jobs.</summary>
    public static readonly (int Min, int Max) Hangfire = (4000, 4049);

    /// <summary>Encina.Quartz — request and notification jobs.</summary>
    public static readonly (int Min, int Max) Quartz = (4050, 4099);

    // ═══════════════════════════════════════════════════════════════════════
    // Messaging transports and API integrations (4100-4699)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.RabbitMQ — message transport.</summary>
    public static readonly (int Min, int Max) RabbitMQ = (4100, 4149);

    /// <summary>Encina.Kafka — message transport.</summary>
    public static readonly (int Min, int Max) Kafka = (4150, 4199);

    /// <summary>Encina.NATS — message transport.</summary>
    public static readonly (int Min, int Max) NATS = (4200, 4249);

    /// <summary>Encina.MQTT — message transport.</summary>
    public static readonly (int Min, int Max) MQTT = (4250, 4299);

    /// <summary>Encina.AzureServiceBus — message transport.</summary>
    public static readonly (int Min, int Max) AzureServiceBus = (4300, 4349);

    /// <summary>Encina.AmazonSQS — message transport.</summary>
    public static readonly (int Min, int Max) AmazonSQS = (4350, 4399);

    /// <summary>Encina.Redis.PubSub — message transport.</summary>
    public static readonly (int Min, int Max) RedisPubSub = (4400, 4449);

    /// <summary>Encina.InMemory — in-memory message transport.</summary>
    public static readonly (int Min, int Max) InMemory = (4450, 4499);

    /// <summary>Encina.gRPC — request/response transport.</summary>
    public static readonly (int Min, int Max) GRpc = (4500, 4549);

    /// <summary>Encina.GraphQL — query and mutation bridge.</summary>
    public static readonly (int Min, int Max) GraphQL = (4550, 4599);

    /// <summary>Encina.SignalR — real-time notification broadcasting.</summary>
    public static readonly (int Min, int Max) SignalR = (4600, 4649);

    /// <summary>Encina.Refit — REST API request handler.</summary>
    public static readonly (int Min, int Max) Refit = (4650, 4699);

    // ═══════════════════════════════════════════════════════════════════════
    // Serverless hosts (4700-4799)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.AwsLambda — Lambda execution and triggers.</summary>
    public static readonly (int Min, int Max) AwsLambda = (4700, 4749);

    /// <summary>Encina.AzureFunctions — Functions execution and triggers.</summary>
    public static readonly (int Min, int Max) AzureFunctions = (4750, 4799);

    // ═══════════════════════════════════════════════════════════════════════
    // Change data capture (4800-4999)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Cdc — CDC processor, sharded connectors, messaging bridge and cache invalidation.</summary>
    public static readonly (int Min, int Max) Cdc = (4800, 4899);

    /// <summary>Encina.Cdc.SqlServer — SQL Server CDC connector.</summary>
    public static readonly (int Min, int Max) CdcSqlServer = (4900, 4949);

    /// <summary>Encina.Cdc.Debezium — Debezium connector.</summary>
    public static readonly (int Min, int Max) CdcDebezium = (4950, 4999);

    // ═══════════════════════════════════════════════════════════════════════
    // Security runtime packages (5000-5399)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Security.Audit — audit pipeline and retention (read audit has <see cref="SecurityAuditRead"/>).</summary>
    public static readonly (int Min, int Max) SecurityAudit = (5000, 5099);

    /// <summary>Encina.Security.Secrets — secret readers, injection, rotation, caching, failover and health checks.</summary>
    public static readonly (int Min, int Max) SecuritySecrets = (5100, 5199);

    /// <summary>Encina.Security.Secrets.AzureKeyVault — Azure Key Vault secret provider.</summary>
    public static readonly (int Min, int Max) SecuritySecretsAzureKeyVault = (5200, 5249);

    /// <summary>Encina.Security.Secrets.AwsSecretsManager — AWS Secrets Manager provider.</summary>
    public static readonly (int Min, int Max) SecuritySecretsAwsSecretsManager = (5250, 5299);

    /// <summary>Encina.Security.Secrets.HashiCorpVault — HashiCorp Vault provider.</summary>
    public static readonly (int Min, int Max) SecuritySecretsHashiCorpVault = (5300, 5349);

    /// <summary>Encina.Security.Secrets.GoogleCloudSecretManager — Google Cloud Secret Manager provider.</summary>
    public static readonly (int Min, int Max) SecuritySecretsGoogleCloudSecretManager = (5350, 5399);

    // ═══════════════════════════════════════════════════════════════════════
    // Observability (7000-7099)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.OpenTelemetry — resharding and observability log messages.</summary>
    public static readonly (int Min, int Max) OpenTelemetry = (7000, 7099);

    // ═══════════════════════════════════════════════════════════════════════
    // Security packages (8000-8099)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Security — core authorization pipeline.</summary>
    public static readonly (int Min, int Max) Security = (8000, 8009);

    /// <summary>Encina.Security.PII — PII masking operations.</summary>
    public static readonly (int Min, int Max) SecurityPII = (8010, 8029);

    /// <summary>Encina.IdGeneration — distributed ID generation.</summary>
    public static readonly (int Min, int Max) IdGeneration = (8030, 8099);

    // ═══════════════════════════════════════════════════════════════════════
    // Compliance packages (8100-8999)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Compliance.GDPR — GDPR compliance pipeline.</summary>
    public static readonly (int Min, int Max) ComplianceGDPR = (8100, 8199);

    /// <summary>Encina.Compliance.Consent — consent management.</summary>
    public static readonly (int Min, int Max) ComplianceConsent = (8200, 8299);

    /// <summary>Encina.Compliance.DataSubjectRights — data subject rights (Art. 15-22).</summary>
    public static readonly (int Min, int Max) ComplianceDSR = (8300, 8349);

    /// <summary>Encina.Compliance.LawfulBasis — lawful basis validation (Art. 6).</summary>
    public static readonly (int Min, int Max) ComplianceLawfulBasis = (8350, 8399);

    /// <summary>Encina.Compliance.Anonymization — data anonymization.</summary>
    public static readonly (int Min, int Max) ComplianceAnonymization = (8400, 8449);

    /// <summary>Encina.Marten.GDPR — crypto-shredding for GDPR.</summary>
    public static readonly (int Min, int Max) MartenGDPRCryptoShredding = (8450, 8499);

    /// <summary>Encina.Compliance.Retention — data retention enforcement.</summary>
    public static readonly (int Min, int Max) ComplianceRetention = (8500, 8599);

    /// <summary>Encina.Compliance.DataResidency — data residency compliance.</summary>
    public static readonly (int Min, int Max) ComplianceDataResidency = (8600, 8699);

    /// <summary>Encina.Compliance.BreachNotification — breach notification (Art. 33-34).</summary>
    public static readonly (int Min, int Max) ComplianceBreachNotification = (8700, 8799);

    /// <summary>Encina.Compliance.DPIA — Data Protection Impact Assessment (Art. 35).</summary>
    public static readonly (int Min, int Max) ComplianceDPIA = (8800, 8899);

    /// <summary>Encina.Compliance.PrivacyByDesign — privacy by design (Art. 25).</summary>
    public static readonly (int Min, int Max) CompliancePrivacyByDesign = (8900, 8949);

    // ═══════════════════════════════════════════════════════════════════════
    // Security extensions (9000-9199)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Security.ABAC — attribute-based access control.</summary>
    public static readonly (int Min, int Max) SecurityABAC = (9000, 9099);

    /// <summary>Encina.Security.AntiTampering — signature validation.</summary>
    public static readonly (int Min, int Max) SecurityAntiTampering = (9100, 9199);

    // ═══════════════════════════════════════════════════════════════════════
    // Compliance extensions (9200-9499)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Compliance.NIS2 — NIS2 Directive cybersecurity compliance.</summary>
    public static readonly (int Min, int Max) ComplianceNIS2 = (9200, 9299);

    /// <summary>Encina.Compliance.CrossBorderTransfer — cross-border transfer (Ch. V).</summary>
    public static readonly (int Min, int Max) ComplianceCrossBorderTransfer = (9300, 9399);

    /// <summary>Encina.Compliance.ProcessorAgreements — processor agreement management (Art. 28).</summary>
    public static readonly (int Min, int Max) ComplianceProcessorAgreements = (9400, 9499);

    // ═══════════════════════════════════════════════════════════════════════
    // AI Act & future compliance modules (9500-9999)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Encina.Compliance.AIAct — EU AI Act compliance pipeline.</summary>
    public static readonly (int Min, int Max) ComplianceAIAct = (9500, 9599);

    /// <summary>Encina.Compliance.Attestation — tamper-evident audit attestation.</summary>
    public static readonly (int Min, int Max) ComplianceAttestation = (9600, 9699);

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
