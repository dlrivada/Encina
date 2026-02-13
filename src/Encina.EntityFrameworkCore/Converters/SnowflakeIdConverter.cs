using Encina.IdGeneration;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Encina.EntityFrameworkCore.Converters;

/// <summary>
/// EF Core value converter for <see cref="SnowflakeId"/> stored as <see cref="long"/>.
/// </summary>
/// <remarks>
/// <para>Column type recommendations:</para>
/// <list type="bullet">
/// <item><description>SQL Server: <c>BIGINT</c></description></item>
/// <item><description>PostgreSQL: <c>BIGINT</c></description></item>
/// <item><description>MySQL: <c>BIGINT</c></description></item>
/// <item><description>SQLite: <c>INTEGER</c></description></item>
/// </list>
/// </remarks>
public sealed class SnowflakeIdConverter : ValueConverter<SnowflakeId, long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnowflakeIdConverter"/> class.
    /// </summary>
    public SnowflakeIdConverter()
        : base(
            id => id.Value,
            value => new SnowflakeId(value))
    {
    }
}
