namespace Encina.DomainModeling.Auditing;

/// <summary>
/// Represents the type of action performed on an entity that is being audited.
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// The entity was created.
    /// </summary>
    Created = 0,

    /// <summary>
    /// The entity was updated.
    /// </summary>
    Updated = 1,

    /// <summary>
    /// The entity was deleted.
    /// </summary>
    Deleted = 2
}
