namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// Implemented by a request that declares the identifier of the resource it accesses, so the decision
/// audit can record it as the entity id of the audit entry.
/// </summary>
/// <remarks>
/// <para>
/// Only explicit resource ids are recorded: this interface, or an attribute named by
/// <c>ResourceIdAttributeName</c> from the resource attribute provider. A request that declares
/// neither leaves the entity id of its audit entry empty; the audit never guesses an id from the
/// request's property names.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record GetPatientRecordQuery(Guid PatientId) : IQuery&lt;PatientRecord&gt;, IABACResourceIdentity
/// {
///     public string? ResourceId =&gt; PatientId.ToString();
/// }
/// </code>
/// </example>
public interface IABACResourceIdentity
{
    /// <summary>
    /// The identifier of the resource the request accesses, or <c>null</c> when this request has none
    /// (for example a list or search request).
    /// </summary>
    string? ResourceId { get; }
}
