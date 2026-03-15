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
    /// Gets or sets a value indicating whether to use the Read Audit Store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, registers <see cref="Security.Audit.IReadAuditStore"/> implemented by
    /// <c>ReadAuditStoreMongoDB</c>.
    /// </para>
    /// <para>
    /// The read audit store tracks who accessed sensitive data, when, and for what purpose,
    /// supporting GDPR Art. 15, HIPAA, SOX, and PCI-DSS compliance requirements.
    /// </para>
    /// </remarks>
    public bool UseReadAuditStore { get; set; }

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
    /// Gets or sets a value indicating whether to use the Retention stores.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, registers <see cref="Compliance.Retention.IRetentionPolicyStore"/>,
    /// <see cref="Compliance.Retention.IRetentionRecordStore"/>,
    /// <see cref="Compliance.Retention.ILegalHoldStore"/>, and
    /// <see cref="Compliance.Retention.IRetentionAuditStore"/>
    /// implemented by their MongoDB store classes for GDPR Article 5(1)(e) compliant
    /// data retention management and legal hold enforcement.
    /// </para>
    /// </remarks>
    public bool UseRetention { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the Data Residency stores.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, registers <see cref="Compliance.DataResidency.IDataLocationStore"/>,
    /// <see cref="Compliance.DataResidency.IResidencyPolicyStore"/>, and
    /// <see cref="Compliance.DataResidency.IResidencyAuditStore"/>
    /// implemented by their MongoDB store classes for data sovereignty and residency
    /// enforcement per GDPR Articles 44–49 cross-border transfer rules.
    /// </para>
    /// </remarks>
    public bool UseDataResidency { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the Breach Notification stores.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, registers <see cref="Compliance.BreachNotification.IBreachRecordStore"/>
    /// and <see cref="Compliance.BreachNotification.IBreachAuditStore"/>
    /// implemented by their MongoDB store classes for GDPR Articles 33–34 compliant
    /// personal data breach notification management and audit trail.
    /// </para>
    /// </remarks>
    public bool UseBreachNotification { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the Processor Agreements stores.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, registers <see cref="Compliance.ProcessorAgreements.IProcessorRegistry"/>,
    /// <see cref="Compliance.ProcessorAgreements.IDPAStore"/>, and
    /// <see cref="Compliance.ProcessorAgreements.IProcessorAuditStore"/>
    /// implemented by their MongoDB store classes for GDPR Article 28 compliant
    /// processor agreement management, DPA lifecycle tracking, and audit trail.
    /// </para>
    /// </remarks>
    public bool UseProcessorAgreements { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the DPIA (Data Protection Impact Assessment) stores.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, registers <see cref="Compliance.DPIA.IDPIAStore"/>
    /// and <see cref="Compliance.DPIA.IDPIAAuditStore"/>
    /// implemented by their MongoDB store classes for GDPR Article 35 compliant
    /// data protection impact assessment management and audit trail.
    /// </para>
    /// </remarks>
    public bool UseDPIA { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the ABAC Policy Store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, registers <see cref="Security.ABAC.Persistence.IPolicyStore"/>
    /// implemented by <c>PolicyStoreMongo</c> for persistent XACML policy storage
    /// using native BSON document storage (no JSON serialization).
    /// </para>
    /// </remarks>
    public bool UseABACPolicyStore { get; set; }

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
    /// Gets or sets the collection name for read audit entries.
    /// </summary>
    public string ReadAuditEntries { get; set; } = "read_audit_entries";

    /// <summary>
    /// Gets or sets the collection name for lawful basis registrations.
    /// </summary>
    public string LawfulBasisRegistrations { get; set; } = "lawful_basis_registrations";

    /// <summary>
    /// Gets or sets the collection name for LIA records.
    /// </summary>
    public string LIARecords { get; set; } = "lia_records";

    /// <summary>
    /// Gets or sets the collection name for token mappings (anonymization).
    /// </summary>
    public string TokenMappings { get; set; } = "token_mappings";

    /// <summary>
    /// Gets or sets the collection name for retention policies.
    /// </summary>
    public string RetentionPolicies { get; set; } = "retention_policies";

    /// <summary>
    /// Gets or sets the collection name for retention records.
    /// </summary>
    public string RetentionRecords { get; set; } = "retention_records";

    /// <summary>
    /// Gets or sets the collection name for legal holds.
    /// </summary>
    public string LegalHolds { get; set; } = "legal_holds";

    /// <summary>
    /// Gets or sets the collection name for retention audit entries.
    /// </summary>
    public string RetentionAuditEntries { get; set; } = "retention_audit_entries";

    /// <summary>
    /// Gets or sets the collection name for data location records (data residency).
    /// </summary>
    public string DataLocations { get; set; } = "data_locations";

    /// <summary>
    /// Gets or sets the collection name for residency policies (data residency).
    /// </summary>
    public string ResidencyPolicies { get; set; } = "residency_policies";

    /// <summary>
    /// Gets or sets the collection name for residency audit entries (data residency).
    /// </summary>
    public string ResidencyAuditEntries { get; set; } = "residency_audit_entries";

    /// <summary>
    /// Gets or sets the collection name for breach records (breach notification).
    /// </summary>
    public string BreachRecords { get; set; } = "breach_records";

    /// <summary>
    /// Gets or sets the collection name for breach phased reports (breach notification).
    /// </summary>
    public string BreachPhasedReports { get; set; } = "breach_phased_reports";

    /// <summary>
    /// Gets or sets the collection name for breach audit entries (breach notification).
    /// </summary>
    public string BreachAuditEntries { get; set; } = "breach_audit_entries";

    /// <summary>
    /// Gets or sets the collection name for processors (processor agreements).
    /// </summary>
    public string Processors { get; set; } = "processors";

    /// <summary>
    /// Gets or sets the collection name for data processing agreements.
    /// </summary>
    public string ProcessorAgreements { get; set; } = "processor_agreements";

    /// <summary>
    /// Gets or sets the collection name for processor agreement audit entries.
    /// </summary>
    public string ProcessorAgreementAuditEntries { get; set; } = "processor_agreement_audit_entries";

    /// <summary>
    /// Gets or sets the collection name for DPIA assessments.
    /// </summary>
    public string DPIAAssessments { get; set; } = "dpia_assessments";

    /// <summary>
    /// Gets or sets the collection name for DPIA audit entries.
    /// </summary>
    public string DPIAAuditEntries { get; set; } = "dpia_audit_entries";

    /// <summary>
    /// Gets or sets the collection name for ABAC policy sets.
    /// </summary>
    public string ABACPolicySets { get; set; } = "abac_policy_sets";

    /// <summary>
    /// Gets or sets the collection name for ABAC standalone policies.
    /// </summary>
    public string ABACPolicies { get; set; } = "abac_policies";
}
