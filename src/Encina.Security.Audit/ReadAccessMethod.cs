namespace Encina.Security.Audit;

/// <summary>
/// Describes how a sensitive entity was accessed during a read operation.
/// </summary>
/// <remarks>
/// <para>
/// This enum categorizes the mechanism through which data was read, providing
/// context for compliance reporting and security analysis. Knowing the access
/// method helps identify unusual access patterns and enforce access policies.
/// </para>
/// <para>
/// Used in <see cref="ReadAuditEntry.AccessMethod"/> to record the access vector
/// for each audited read operation.
/// </para>
/// </remarks>
public enum ReadAccessMethod
{
    /// <summary>
    /// Data was accessed through the repository pattern.
    /// </summary>
    /// <remarks>
    /// Indicates the read was performed via <c>IRepository&lt;TEntity, TId&gt;</c>
    /// or <c>IReadOnlyRepository&lt;TEntity, TId&gt;</c>. This is the most common
    /// access method when read auditing is enabled via repository decorators.
    /// </remarks>
    Repository = 0,

    /// <summary>
    /// Data was accessed through a direct database query.
    /// </summary>
    /// <remarks>
    /// Indicates the read bypassed the repository pattern and used direct database
    /// access (e.g., raw SQL, <c>DbContext</c> queries, or Dapper calls).
    /// Useful for manual audit logging when the repository decorator is not in use.
    /// </remarks>
    DirectQuery = 1,

    /// <summary>
    /// Data was accessed through an external API call.
    /// </summary>
    /// <remarks>
    /// Indicates the data was retrieved via an API endpoint (REST, GraphQL, gRPC).
    /// Useful for tracking cross-service data access in microservice architectures.
    /// </remarks>
    Api = 2,

    /// <summary>
    /// Data was accessed for export or data portability purposes.
    /// </summary>
    /// <remarks>
    /// Indicates the read was performed as part of a data export operation,
    /// such as GDPR Art. 20 (right to data portability) or reporting.
    /// These reads may involve bulk data extraction and deserve heightened scrutiny.
    /// </remarks>
    Export = 3,

    /// <summary>
    /// A user-defined access method not covered by the standard categories.
    /// </summary>
    /// <remarks>
    /// Use this value for application-specific access patterns that don't fit
    /// the predefined categories. Additional context can be provided via
    /// <see cref="ReadAuditEntry.Metadata"/>.
    /// </remarks>
    Custom = 4
}
