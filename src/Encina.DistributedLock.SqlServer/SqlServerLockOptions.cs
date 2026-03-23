using System.Text.Json.Serialization;

namespace Encina.DistributedLock.SqlServer;

/// <summary>
/// Options for the SQL Server distributed lock provider.
/// </summary>
public sealed class SqlServerLockOptions : DistributedLockOptions
{
    /// <summary>
    /// Gets or sets the SQL Server connection string.
    /// </summary>
    /// <remarks>
    /// WARNING: Contains sensitive credential data. Never log or serialize.
    /// </remarks>
    [JsonIgnore]
    public string? ConnectionString { get; set; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"SqlServerLockOptions {{ Prefix={KeyPrefix}, Expiry={DefaultExpiry} }}";
}
