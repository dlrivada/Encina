using System.Data;
using System.Data.Common;

namespace Encina.IntegrationTests.Messaging.DeadLetter;

/// <summary>
/// Moves a PostgreSQL session to a non-UTC time zone for the boundary facts of the dead letter store contract
/// (maintainer decision B4): a <c>TIMESTAMPTZ</c> comparison that depended on the session zone would show up here.
/// </summary>
internal static class PostgreSqlSessionTimeZone
{
    private const string Zone = "America/Los_Angeles";

    /// <summary>Sets the zone on <paramref name="connection"/> and checks that the session reports it.</summary>
    public static async Task ApplyAsync(IDbConnection connection)
    {
        var db = (DbConnection)connection;
        if (db.State != ConnectionState.Open)
        {
            await db.OpenAsync();
        }

        await using (var set = db.CreateCommand())
        {
            set.CommandText = $"SET TIME ZONE '{Zone}'";
            await set.ExecuteNonQueryAsync();
        }

        await using var show = db.CreateCommand();
        show.CommandText = "SHOW TIME ZONE";
        var actual = (string?)await show.ExecuteScalarAsync();
        if (!string.Equals(actual, Zone, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The session time zone is '{actual}', not '{Zone}'.");
        }
    }
}
