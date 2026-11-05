namespace Encina.Audit.Marten.Resolution;

/// <summary>
/// The audit trail an audit event belongs to. Each trail has its own event type, projection and field set.
/// </summary>
public enum AuditResolutionTrail
{
    /// <summary>The operation (write) audit trail.</summary>
    Operation = 0,

    /// <summary>The read-access audit trail.</summary>
    Read = 1,
}
