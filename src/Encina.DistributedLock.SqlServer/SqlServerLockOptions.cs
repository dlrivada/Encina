namespace Encina.DistributedLock.SqlServer;

/// <summary>
/// Options for the SQL Server distributed lock provider.
/// </summary>
public sealed class SqlServerLockOptions : DistributedLockOptions
{
    /// <summary>
    /// Gets or sets the SQL Server connection string.
    /// </summary>
    public string? ConnectionString { get; set; }
}
