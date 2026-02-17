using System.Data;
using Encina.IdGeneration;

namespace Encina.ADO.PostgreSQL;

/// <summary>
/// Extension methods for adding Encina ID generation type parameters to ADO.NET commands
/// with correct <see cref="DbType"/> mappings for PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL has native support for <c>BIGINT</c>, <c>UUID</c>, and <c>TEXT</c>/<c>VARCHAR</c>
/// types. These extension methods map each Encina ID type to the most appropriate PostgreSQL type:
/// <list type="bullet">
///   <item><see cref="SnowflakeId"/> maps to <see cref="DbType.Int64"/> (<c>BIGINT</c>).</item>
///   <item><see cref="UlidId"/> maps to <see cref="DbType.Guid"/> (<c>UUID</c>).</item>
///   <item><see cref="UuidV7Id"/> maps to <see cref="DbType.Guid"/> (<c>UUID</c>).</item>
///   <item><see cref="ShardPrefixedId"/> maps to <see cref="DbType.String"/> (<c>TEXT</c>).</item>
/// </list>
/// </para>
/// </remarks>
public static class IdParameterExtensions
{
    /// <summary>
    /// Adds a <see cref="SnowflakeId"/> parameter to the command.
    /// </summary>
    /// <param name="command">The ADO.NET command to add the parameter to.</param>
    /// <param name="name">The parameter name (e.g., <c>@Id</c>).</param>
    /// <param name="value">The <see cref="SnowflakeId"/> value.</param>
    /// <remarks>
    /// Maps to PostgreSQL column type <c>BIGINT</c>. The raw 64-bit value is stored directly.
    /// </remarks>
    public static void AddSnowflakeIdParameter(this IDbCommand command, string name, SnowflakeId value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value.Value;
        parameter.DbType = DbType.Int64;
        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Adds a <see cref="UlidId"/> parameter to the command.
    /// </summary>
    /// <param name="command">The ADO.NET command to add the parameter to.</param>
    /// <param name="name">The parameter name (e.g., <c>@Id</c>).</param>
    /// <param name="value">The <see cref="UlidId"/> value.</param>
    /// <remarks>
    /// Maps to PostgreSQL column type <c>UUID</c>. The ULID is converted to a <see cref="Guid"/>
    /// via <see cref="UlidId.ToGuid()"/> for native 16-byte storage.
    /// </remarks>
    public static void AddUlidIdParameter(this IDbCommand command, string name, UlidId value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value.ToGuid();
        parameter.DbType = DbType.Guid;
        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Adds a <see cref="UuidV7Id"/> parameter to the command.
    /// </summary>
    /// <param name="command">The ADO.NET command to add the parameter to.</param>
    /// <param name="name">The parameter name (e.g., <c>@Id</c>).</param>
    /// <param name="value">The <see cref="UuidV7Id"/> value.</param>
    /// <remarks>
    /// Maps to PostgreSQL column type <c>UUID</c>. The underlying <see cref="Guid"/>
    /// is stored directly in the native 16-byte format.
    /// </remarks>
    public static void AddUuidV7IdParameter(this IDbCommand command, string name, UuidV7Id value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value.Value;
        parameter.DbType = DbType.Guid;
        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Adds a <see cref="ShardPrefixedId"/> parameter to the command.
    /// </summary>
    /// <param name="command">The ADO.NET command to add the parameter to.</param>
    /// <param name="name">The parameter name (e.g., <c>@Id</c>).</param>
    /// <param name="value">The <see cref="ShardPrefixedId"/> value.</param>
    /// <remarks>
    /// Maps to PostgreSQL column type <c>TEXT</c>. The full shard-prefixed string
    /// (e.g., <c>shard-01:01ARZ3NDEKTSV4RRFFQ69G5FAV</c>) is stored as-is.
    /// </remarks>
    public static void AddShardPrefixedIdParameter(this IDbCommand command, string name, ShardPrefixedId value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value.ToString();
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);
    }
}
