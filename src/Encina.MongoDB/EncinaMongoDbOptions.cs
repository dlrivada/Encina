using Encina.Messaging.Health;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.MongoDB.Modules;
using Encina.MongoDB.ReadWriteSeparation;

namespace Encina.MongoDB;

/// <summary>
/// Configuration options for Encina MongoDB integration.
/// </summary>
public sealed class EncinaMongoDbOptions
{
    /// <summary>
    /// Gets or sets the MongoDB connection string.
    /// </summary>
    /// <example>mongodb://localhost:27017</example>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string DatabaseName { get; set; } = "Encina";

    /// <summary>
    /// Gets or sets the collection names for messaging patterns.
    /// </summary>
    public MongoDbCollectionNames Collections { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to use the Outbox pattern.
    /// </summary>
    public bool UseOutbox { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the Inbox pattern.
    /// </summary>
    public bool UseInbox { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the Saga pattern.
    /// </summary>
    public bool UseSagas { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the Scheduling pattern.
    /// </summary>
    public bool UseScheduling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the Audit Log Store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, registers IAuditLogStore implemented by AuditLogStoreMongoDB.
    /// </para>
    /// <para>
    /// The audit log store provides a persistent backing for detailed audit trail tracking,
    /// capturing entity changes, user actions, and correlation information.
    /// </para>
    /// </remarks>
    public bool UseAuditLogStore { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the Security Audit Store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, registers IAuditStore implemented by AuditStoreMongoDB.
    /// </para>
    /// <para>
    /// The security audit store provides comprehensive audit trail for operations
    /// including request/response payloads, outcome tracking, and compliance features.
    /// </para>
    /// </remarks>
    public bool UseSecurityAuditStore { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the Consent stores.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, registers <see cref="Compliance.Consent.IConsentStore"/>,
    /// <see cref="Compliance.Consent.IConsentAuditStore"/>, and
    /// <see cref="Compliance.Consent.IConsentVersionManager"/> implemented by their
    /// respective MongoDB store classes.
    /// </para>
    /// <para>
    /// The consent stores provide GDPR-compliant consent lifecycle management backed
    /// by MongoDB collections for consent records, audit trail, and version tracking.
    /// </para>
    /// </remarks>
    public bool UseConsent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the Data Subject Rights stores.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, registers <see cref="Compliance.DataSubjectRights.IDSRRequestStore"/>
    /// and <see cref="Compliance.DataSubjectRights.IDSRAuditStore"/> implemented by their
    /// respective MongoDB store classes for GDPR Articles 15-22 compliance.
    /// </para>
    /// </remarks>
    public bool UseDataSubjectRights { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the Anonymization stores.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, registers <see cref="Compliance.Anonymization.ITokenMappingStore"/>
    /// implemented by its MongoDB store class for GDPR-compliant data anonymization
    /// and pseudonymization via tokenization.
    /// </para>
    /// </remarks>
    public bool UseAnonymization { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to create indexes automatically.
    /// </summary>
    public bool CreateIndexes { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable module isolation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, MongoDB operations are routed to module-specific databases
    /// based on the current module execution context.
    /// </para>
    /// <para>
    /// Configure module isolation options via <see cref="ModuleIsolationOptions"/>.
    /// </para>
    /// </remarks>
    public bool UseModuleIsolation { get; set; }

    /// <summary>
    /// Gets the module isolation options.
    /// </summary>
    /// <remarks>
    /// Only used when <see cref="UseModuleIsolation"/> is <c>true</c>.
    /// </remarks>
    public MongoDbModuleIsolationOptions ModuleIsolationOptions { get; } = new();

    /// <summary>
    /// Gets or sets the saga options.
    /// </summary>
    public SagaOptions SagaOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the scheduling options.
    /// </summary>
    public SchedulingOptions SchedulingOptions { get; set; } = new();

    /// <summary>
    /// Gets the provider health check options.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to enable read/write separation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, query operations (<c>IQuery&lt;T&gt;</c>) are routed to secondary
    /// members of the replica set using the configured read preference, while command
    /// operations (<c>ICommand&lt;T&gt;</c>) always use the primary.
    /// </para>
    /// <para>
    /// <b>Requirements:</b>
    /// Read/write separation requires a MongoDB replica set deployment. Standalone
    /// MongoDB servers do not support read preferences other than Primary.
    /// </para>
    /// <para>
    /// Configure read/write separation options via <see cref="ReadWriteSeparationOptions"/>.
    /// </para>
    /// </remarks>
    public bool UseReadWriteSeparation { get; set; }

    /// <summary>
    /// Gets the read/write separation options.
    /// </summary>
    /// <remarks>
    /// Only used when <see cref="UseReadWriteSeparation"/> is <c>true</c>.
    /// </remarks>
    public MongoReadWriteSeparationOptions ReadWriteSeparationOptions { get; } = new();
}

/// <summary>
/// Collection names for MongoDB messaging patterns.
/// </summary>
public sealed class MongoDbCollectionNames
{
    /// <summary>
    /// Gets or sets the collection name for outbox messages.
    /// </summary>
    public string Outbox { get; set; } = "outbox_messages";

    /// <summary>
    /// Gets or sets the collection name for inbox messages.
    /// </summary>
    public string Inbox { get; set; } = "inbox_messages";

    /// <summary>
    /// Gets or sets the collection name for saga states.
    /// </summary>
    public string Sagas { get; set; } = "saga_states";

    /// <summary>
    /// Gets or sets the collection name for scheduled messages.
    /// </summary>
    public string ScheduledMessages { get; set; } = "scheduled_messages";

    /// <summary>
    /// Gets or sets the collection name for audit logs.
    /// </summary>
    public string AuditLogs { get; set; } = "audit_logs";

    /// <summary>
    /// Gets or sets the collection name for security audit entries.
    /// </summary>
    public string SecurityAuditEntries { get; set; } = "security_audit_entries";

    /// <summary>
    /// Gets or sets the collection name for consent records.
    /// </summary>
    public string Consents { get; set; } = "consents";

    /// <summary>
    /// Gets or sets the collection name for consent audit entries.
    /// </summary>
    public string ConsentAuditEntries { get; set; } = "consent_audit";

    /// <summary>
    /// Gets or sets the collection name for consent versions.
    /// </summary>
    public string ConsentVersions { get; set; } = "consent_versions";

    /// <summary>
    /// Gets or sets the collection name for lawful basis registrations.
    /// </summary>
    public string LawfulBasisRegistrations { get; set; } = "lawful_basis_registrations";

    /// <summary>
    /// Gets or sets the collection name for LIA records.
    /// </summary>
    public string LIARecords { get; set; } = "lia_records";

    /// <summary>
    /// Gets or sets the collection name for DSR (Data Subject Rights) requests.
    /// </summary>
    public string DSRRequests { get; set; } = "dsr_requests";

    /// <summary>
    /// Gets or sets the collection name for DSR audit entries.
    /// </summary>
    public string DSRAuditEntries { get; set; } = "dsr_audit_entries";

    /// <summary>
    /// Gets or sets the collection name for token mappings (anonymization).
    /// </summary>
    public string TokenMappings { get; set; } = "token_mappings";
}
