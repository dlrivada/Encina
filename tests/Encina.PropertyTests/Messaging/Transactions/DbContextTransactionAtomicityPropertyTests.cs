using Encina.EntityFrameworkCore.Transactions;
using Encina.Messaging.Transactions;
using FsCheck.Xunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.PropertyTests.Messaging.Transactions;

/// <summary>
/// Property (i) of #718 against the real <see cref="DbContextTransactionAccessor"/> over SQLite in memory: for any
/// sequence of joins, commits and rollbacks the transaction commits if and only if the owner commits and no
/// participant rolled back, observed through a row written inside the transaction.
/// </summary>
[Trait("Category", "Property")]
public sealed class DbContextTransactionAtomicityPropertyTests
{
    private sealed class Row
    {
        public int Id { get; set; }
    }

    private sealed class Db(DbContextOptions<Db> options) : DbContext(options)
    {
        public DbSet<Row> Rows => Set<Row>();
    }

    private enum Step
    {
        Join,
        CommitLastJoiner,
        RollbackLastJoiner,
        DisposeLastJoiner,
        CommitAllJoiners
    }

    [Property(MaxTest = 100)]
    public async Task<bool> Property_TransactionCommitsIffOwnerCommitsAndNoParticipantRolledBack(byte[] steps, bool ownerCommits)
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<Db>().UseSqlite(connection).Options;
        await using var db = new Db(options);
        await db.Database.EnsureCreatedAsync();
        using var accessor = new DbContextTransactionAccessor(db, NullLogger<DbContextTransactionAccessor>.Instance);

        var owner = (await ((ITransactionLeaseSource)accessor).BeginOrJoinAsync()).ShouldBeRight();
        db.Rows.Add(new Row());
        await db.SaveChangesAsync();

        var joiners = new List<ITransactionLease>();
        var anyRollback = false;
        foreach (var raw in steps ?? [])
        {
            switch ((Step)(raw % 5))
            {
                case Step.Join:
                    joiners.Add((await ((ITransactionLeaseSource)accessor).BeginOrJoinAsync()).ShouldBeRight());
                    break;
                case Step.CommitLastJoiner when joiners.Count > 0:
                    (await joiners[^1].CommitAsync()).ShouldBeRight();
                    break;
                case Step.RollbackLastJoiner when joiners.Count > 0:
                    await joiners[^1].RollbackAsync();
                    anyRollback = true;
                    break;
                case Step.DisposeLastJoiner when joiners.Count > 0:
                    await joiners[^1].DisposeAsync();
                    joiners.RemoveAt(joiners.Count - 1);
                    break;
                case Step.CommitAllJoiners:
                    foreach (var joiner in joiners)
                    {
                        (await joiner.CommitAsync()).ShouldBeRight();
                    }

                    break;
            }
        }

        foreach (var joiner in joiners)
        {
            await joiner.DisposeAsync();
        }

        var expectedCommit = ownerCommits && !anyRollback;
        if (ownerCommits)
        {
            if ((await owner.CommitAsync()).IsRight != expectedCommit)
            {
                return false;
            }
        }
        else
        {
            await owner.RollbackAsync();
        }

        await owner.DisposeAsync();
        await using var verify = new Db(options);
        return await verify.Rows.CountAsync() == (expectedCommit ? 1 : 0)
            && !((ITransactionLeaseSource)accessor).HasActiveTransaction;
    }
}
