using Encina.IdGeneration;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Encina.EntityFrameworkCore.Converters;

/// <summary>
/// EF Core value converter for <see cref="UuidV7Id"/> stored as a <see cref="Guid"/>.
/// </summary>
/// <remarks>
/// <para>Column type recommendations:</para>
/// <list type="bullet">
/// <item><description>SQL Server: <c>UNIQUEIDENTIFIER</c></description></item>
/// <item><description>PostgreSQL: <c>UUID</c></description></item>
/// <item><description>MySQL: <c>CHAR(36)</c></description></item>
/// <item><description>SQLite: <c>TEXT</c></description></item>
/// </list>
/// </remarks>
public sealed class UuidV7IdConverter : ValueConverter<UuidV7Id, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UuidV7IdConverter"/> class.
    /// </summary>
    public UuidV7IdConverter()
        : base(
            id => id.Value,
            value => new UuidV7Id(value))
    {
    }
}
