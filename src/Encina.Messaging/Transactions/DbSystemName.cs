using System.Data;

namespace Encina.Messaging.Transactions;

/// <summary>
/// Maps a connection to its OpenTelemetry <c>db.system</c> value, from the connection's type namespace.
/// </summary>
internal static class DbSystemName
{
    internal const string SqlServer = "mssql";
    internal const string PostgreSql = "postgresql";
    internal const string MySql = "mysql";
    internal const string Other = "other_sql";

    /// <summary>
    /// Gets the <c>db.system</c> value of <paramref name="connection"/>, looking through module-isolation
    /// decorators.
    /// </summary>
    /// <param name="connection">The connection, or <see langword="null"/>.</param>
    /// <returns>The <c>db.system</c> value, or <see cref="Other"/> when the provider is not recognized.</returns>
    internal static string From(IDbConnection? connection)
    {
        while (connection is IWrappedDbConnection wrapper)
        {
            connection = wrapper.InnerConnection;
        }

        var ns = connection?.GetType().Namespace;
        if (ns is null)
        {
            return Other;
        }

        if (ns.Contains("SqlClient", StringComparison.Ordinal))
        {
            return SqlServer;
        }

        if (ns.StartsWith("Npgsql", StringComparison.Ordinal))
        {
            return PostgreSql;
        }

        return ns.StartsWith("MySql", StringComparison.Ordinal) ? MySql : Other;
    }

    /// <summary>
    /// Gets the <c>db.system</c> value of a transaction source: its connection's when it is relational,
    /// <see cref="Other"/> otherwise.
    /// </summary>
    /// <param name="source">The transaction source.</param>
    /// <returns>The <c>db.system</c> value.</returns>
    internal static string From(ITransactionLeaseSource source)
        => source is IDbTransactionAccessor accessor ? From(accessor.Connection) : Other;
}
