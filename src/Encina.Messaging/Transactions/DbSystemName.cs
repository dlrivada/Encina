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
    internal static string From(IDbConnection? connection) => Classify(Innermost(connection)?.GetType().Namespace);

    private static IDbConnection? Innermost(IDbConnection? connection)
    {
        while (connection is IWrappedDbConnection wrapper)
        {
            connection = wrapper.InnerConnection;
        }

        return connection;
    }

    // crap-exempt: single-question switch — maps a connection namespace to its db.system value
    private static string Classify(string? ns) => ns switch
    {
        null => Other,
        _ when ns.Contains("SqlClient", StringComparison.Ordinal) => SqlServer,
        _ when ns.StartsWith("Npgsql", StringComparison.Ordinal) => PostgreSql,
        _ when ns.StartsWith("MySql", StringComparison.Ordinal) => MySql,
        _ => Other
    };
}
