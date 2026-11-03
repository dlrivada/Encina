namespace Encina.Audit.Marten.Resolution;

/// <summary>
/// How an operator resolved an audit event field the projection cannot read.
/// </summary>
public enum AuditResolutionKind
{
    /// <summary>The field cannot be recovered and reads as the unreadable placeholder.</summary>
    Unreadable = 0,

    /// <summary>The field was shredded by a recorded data-erasure request and reads as the shredded placeholder.</summary>
    Shredded = 1,

    /// <summary>The field expired under a recorded retention execution and reads as the expired placeholder.</summary>
    Expired = 2,

    /// <summary>The whole event is excluded from the read model (no row is produced).</summary>
    Excluded = 3,
}
