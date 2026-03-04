namespace Encina.Security.Audit;

/// <summary>
/// Provides contextual information for read audit trail entries.
/// </summary>
/// <remarks>
/// <para>
/// Use this interface to declare the purpose of data access before performing read operations
/// on entities marked with <c>IReadAuditable</c>. The declared purpose is recorded in
/// <see cref="ReadAuditEntry.Purpose"/> for compliance reporting.
/// </para>
/// <para>
/// This is a scoped service — each HTTP request or scope gets its own instance.
/// Set the purpose before calling repository methods to have it captured in the audit trail.
/// </para>
/// <para>
/// Supports GDPR Art. 15 compliance: when a data subject exercises their right of access,
/// the controller must provide information about the purposes of processing. Recording
/// the access purpose at read time creates an auditable trail of data usage.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a query handler or service
/// public class GetPatientHandler
/// {
///     private readonly IReadAuditContext _auditContext;
///     private readonly IReadOnlyRepository&lt;Patient, PatientId&gt; _repository;
///
///     public async Task&lt;Patient?&gt; HandleAsync(GetPatientQuery query)
///     {
///         _auditContext.WithPurpose("Patient care review");
///         return await _repository.GetByIdAsync(query.PatientId);
///     }
/// }
/// </code>
/// </example>
public interface IReadAuditContext
{
    /// <summary>
    /// Gets the declared purpose for the current data access operation.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when no purpose has been declared for the current scope.
    /// When <see cref="ReadAuditOptions.RequirePurpose"/> is <c>true</c>,
    /// a warning is logged for read operations without a declared purpose.
    /// </remarks>
    string? Purpose { get; }

    /// <summary>
    /// Declares the purpose for subsequent read operations in the current scope.
    /// </summary>
    /// <param name="purpose">The access purpose (e.g., "Patient care review", "Compliance audit").</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// The purpose is stored for the duration of the current scope and is included
    /// in all <see cref="ReadAuditEntry"/> records created by the repository decorator.
    /// </para>
    /// <para>
    /// Calling this method multiple times replaces the previous purpose.
    /// </para>
    /// </remarks>
    IReadAuditContext WithPurpose(string purpose);
}
