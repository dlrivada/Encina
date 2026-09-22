using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;

/// <summary>
/// Creates the tables of a <see cref="DbContext"/> in a shared collection database.
/// </summary>
/// <remarks>
/// <c>Database.EnsureCreatedAsync()</c> does nothing when the database already contains any table, which is always
/// true for the databases shared by a <c>[Collection]</c> fixture, so a context whose tables are not part of the
/// fixture's base schema (for example the audit stores) never got them (#1094). This helper creates the context's
/// tables inside a transaction; when they already exist the DDL fails with an "already exists" error, the
/// transaction is rolled back and the existing tables are used as they are; any other DDL error (for example
/// an invalid index filter, #1128) is rethrown.
/// </remarks>
internal static class EFCoreSchema
{
    public static async Task CreateTablesAsync(DbContext context)
    {
        var creator = context.GetService<IRelationalDatabaseCreator>();
        if (!await creator.ExistsAsync())
        {
            await creator.CreateAsync();
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await creator.CreateTablesAsync();
            await transaction.CommitAsync();
        }
        catch (DbException ex) when (IsAlreadyExists(ex))
        {
            // The context's tables already exist (created by an earlier test class or the base schema).
            await transaction.RollbackAsync();
        }
    }

    // PostgreSQL 42P07 duplicate_table, SQL Server 2714 "There is already an object named ...",
    // MySQL 1050 "Table ... already exists". Any other DDL failure is a real error and must surface.
    private static bool IsAlreadyExists(DbException ex) =>
        ex.SqlState == "42P07"
        || ex.Message.Contains("There is already an object named", StringComparison.Ordinal)
        || (ex.Message.Contains("Table '", StringComparison.Ordinal) && ex.Message.Contains("' already exists", StringComparison.Ordinal));
}
