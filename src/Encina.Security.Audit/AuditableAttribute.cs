namespace Encina.Security.Audit;

/// <summary>
/// Configures audit behavior for a request class.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to command or query classes to customize how they are audited.
/// Properties set on the attribute override values extracted from type name conventions.
/// </para>
/// <para>
/// <b>Default Behavior:</b>
/// <list type="bullet">
/// <item>Commands are audited by default (controlled by <c>AuditOptions.AuditAllCommands</c>)</item>
/// <item>Queries are NOT audited by default (controlled by <c>AuditOptions.AuditAllQueries</c>)</item>
/// <item>Entity type and action are extracted from type name (e.g., <c>CreateOrderCommand</c> → Entity: "Order", Action: "Create")</item>
/// </list>
/// </para>
/// <para>
/// <b>Use Cases:</b>
/// <list type="bullet">
/// <item>Override entity type or action when convention doesn't match</item>
/// <item>Opt-out of auditing with <c>Skip = true</c></item>
/// <item>Force auditing of a specific query</item>
/// <item>Set sensitivity level for compliance classification</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Override entity type and action
/// [Auditable(EntityType = "Invoice", Action = "Generate")]
/// public sealed record GenerateInvoicePdfCommand(Guid OrderId) : ICommand;
///
/// // Opt-out of auditing for internal commands
/// [Auditable(Skip = true)]
/// public sealed record UpdateInternalCacheCommand() : ICommand;
///
/// // Force auditing of a sensitive query
/// [Auditable(SensitivityLevel = "High")]
/// public sealed record GetUserPersonalDataQuery(Guid UserId) : IQuery&lt;PersonalDataDto&gt;;
///
/// // Disable payload hashing for large payloads
/// [Auditable(IncludePayloadValue = false)]
/// public sealed record ImportBulkDataCommand(byte[] Data) : ICommand;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AuditableAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the entity type for this request.
    /// </summary>
    /// <remarks>
    /// Overrides the entity type extracted from the type name convention.
    /// If <c>null</c>, the entity type is extracted from the request type name.
    /// </remarks>
    /// <example>
    /// <code>
    /// [Auditable(EntityType = "Invoice")]
    /// public sealed record GeneratePdfCommand(Guid OrderId) : ICommand;
    /// </code>
    /// </example>
    public string? EntityType { get; init; }

    /// <summary>
    /// Gets or sets the action for this request.
    /// </summary>
    /// <remarks>
    /// Overrides the action extracted from the type name convention.
    /// If <c>null</c>, the action is extracted from the request type name.
    /// </remarks>
    /// <example>
    /// <code>
    /// [Auditable(Action = "Archive")]
    /// public sealed record SoftDeleteOrderCommand(Guid OrderId) : ICommand;
    /// </code>
    /// </example>
    public string? Action { get; init; }

    /// <summary>
    /// Gets or sets whether to include the request payload hash in the audit entry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is <c>null</c>, which defers to <c>AuditOptions.IncludePayloadHash</c>.
    /// </para>
    /// <para>
    /// Set to <c>false</c> to disable payload hashing for requests with large payloads
    /// or when the payload contains data that shouldn't be hashed for performance reasons.
    /// </para>
    /// </remarks>
    public bool? IncludePayload => _includePayloadSet ? _includePayload : null;

    /// <summary>
    /// Sets the <see cref="IncludePayload"/> value. Use this in attribute declarations.
    /// </summary>
    /// <remarks>
    /// This property exists because C# attributes cannot use nullable value types directly.
    /// Set to <c>true</c> to include payload hash, <c>false</c> to exclude it.
    /// </remarks>
    /// <example>
    /// <code>
    /// [Auditable(IncludePayloadValue = false)]
    /// public sealed record LargePayloadCommand(byte[] Data) : ICommand;
    /// </code>
    /// </example>
    public bool IncludePayloadValue
    {
        get => _includePayload;
        init
        {
            _includePayload = value;
            _includePayloadSet = true;
        }
    }

    private bool _includePayload;
    private bool _includePayloadSet;

    /// <summary>
    /// Gets or sets the sensitivity level for compliance classification.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Optional field for categorizing audit entries by data sensitivity.
    /// Common values: "Low", "Medium", "High", "Critical", "PII", "PHI", "Financial".
    /// </para>
    /// <para>
    /// This value is stored in <see cref="AuditEntry.Metadata"/> under the key "SensitivityLevel".
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [Auditable(SensitivityLevel = "PHI")]
    /// public sealed record UpdatePatientRecordCommand(Guid PatientId, PatientData Data) : ICommand;
    /// </code>
    /// </example>
    public string? SensitivityLevel { get; init; }

    /// <summary>
    /// Gets or sets whether to skip auditing for this request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the request will not be audited regardless of global settings.
    /// Use this for internal infrastructure commands that don't need audit trails.
    /// </para>
    /// <para>
    /// Default is <c>false</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [Auditable(Skip = true)]
    /// public sealed record RefreshCacheCommand() : ICommand;
    /// </code>
    /// </example>
    public bool Skip { get; init; }
}
