using Encina.IdGeneration;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Encina.EntityFrameworkCore.Converters;

/// <summary>
/// EF Core value converter for <see cref="UlidId"/> stored as a Crockford Base32 <see cref="string"/>.
/// </summary>
/// <remarks>
/// <para>Column type recommendations:</para>
/// <list type="bullet">
/// <item><description>SQL Server: <c>CHAR(26)</c></description></item>
/// <item><description>PostgreSQL: <c>CHAR(26)</c></description></item>
/// <item><description>MySQL: <c>CHAR(26)</c></description></item>
/// <item><description>SQLite: <c>TEXT</c></description></item>
/// </list>
/// </remarks>
public sealed class UlidIdConverter : ValueConverter<UlidId, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UlidIdConverter"/> class.
    /// </summary>
    public UlidIdConverter()
        : base(
            id => id.ToString(),
            value => UlidId.Parse(value))
    {
    }
}
