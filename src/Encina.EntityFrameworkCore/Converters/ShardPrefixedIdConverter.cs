using Encina.IdGeneration;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Encina.EntityFrameworkCore.Converters;

/// <summary>
/// EF Core value converter for <see cref="ShardPrefixedId"/> stored as a <see cref="string"/>.
/// </summary>
/// <remarks>
/// <para>
/// Stores the full shard-prefixed ID string (e.g., <c>shard-01:01ARZ3NDEKTSV4RRFFQ69G5FAV</c>).
/// </para>
/// <para>Column type recommendations:</para>
/// <list type="bullet">
/// <item><description>SQL Server: <c>NVARCHAR(128)</c></description></item>
/// <item><description>PostgreSQL: <c>VARCHAR(128)</c></description></item>
/// <item><description>MySQL: <c>VARCHAR(128)</c></description></item>
/// <item><description>SQLite: <c>TEXT</c></description></item>
/// </list>
/// </remarks>
public sealed class ShardPrefixedIdConverter : ValueConverter<ShardPrefixedId, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShardPrefixedIdConverter"/> class.
    /// </summary>
    public ShardPrefixedIdConverter()
        : base(
            id => id.ToString(),
            value => ShardPrefixedId.Parse(value))
    {
    }
}
