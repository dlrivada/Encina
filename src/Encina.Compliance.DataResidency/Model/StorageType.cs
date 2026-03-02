namespace Encina.Compliance.DataResidency.Model;

/// <summary>
/// Classification of how data is stored in a given region.
/// </summary>
/// <remarks>
/// <para>
/// The storage type affects data residency compliance because different storage
/// mechanisms have different data sovereignty implications. A primary database
/// stores the authoritative copy, while replicas, caches, and backups may create
/// additional copies in different geographic locations.
/// </para>
/// <para>
/// Per GDPR Recital 101, flows of personal data to and from countries outside
/// the Union are necessary for the expansion of international trade. The protection
/// must follow the data regardless of the storage mechanism or location.
/// </para>
/// </remarks>
public enum StorageType
{
    /// <summary>
    /// The authoritative primary storage location for the data.
    /// </summary>
    Primary = 0,

    /// <summary>
    /// A read replica containing a synchronized copy of the data.
    /// </summary>
    Replica = 1,

    /// <summary>
    /// A temporary cached copy of the data for performance optimization.
    /// </summary>
    Cache = 2,

    /// <summary>
    /// A backup copy stored for disaster recovery purposes.
    /// </summary>
    Backup = 3,

    /// <summary>
    /// An archived copy stored for long-term retention or compliance purposes.
    /// </summary>
    Archive = 4
}
