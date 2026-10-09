using System.Data;
using Encina.EntityFrameworkCore;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static Encina.IntegrationTests.Messaging.InboxPipelineScenario;

namespace Encina.IntegrationTests.Messaging;

/// <summary>
/// One <see cref="Harness"/> per provider family for <see cref="InboxPipelineScenario"/>: the registration the
/// application would write, plus a reader of the persisted inbox row on a fresh connection.
/// </summary>
public static class InboxPipelineHarnesses
{
    private static Task<InboxRow?> ToRowAsync(Task<Either<EncinaError, Option<IInboxMessage>>> read) =>
        read.ContinueWith(
            t =>
            {
                var option = t.Result.ShouldBeRight();
                return option.Match(
                    Some: m => (InboxRow?)new InboxRow(m.RetryCount, m.IsProcessed, m.Response),
                    None: () => null);
            },
            TaskScheduler.Default);

    private static Task CloseScopedConnection(IServiceProvider scope)
    {
        scope.GetRequiredService<IDbConnection>().Close();
        return Task.CompletedTask;
    }

    /// <summary>ADO.NET over SQL Server.</summary>
    public static Harness AdoSqlServer(SqlServerFixture f) => new()
    {
        Register = (s, setup) =>
        {
            s.AddScoped<IDbConnection>(_ => f.CreateConnection());
            global::Encina.ADO.SqlServer.ServiceCollectionExtensions.AddEncinaADO(s, c => Configure(c, setup));
        },
        ReadRow = id => ToRowAsync(new global::Encina.ADO.SqlServer.Inbox.InboxStoreADO(f.CreateConnection()).GetMessageAsync(id)),
        BreakTransaction = CloseScopedConnection
    };

    /// <summary>ADO.NET over PostgreSQL.</summary>
    public static Harness AdoPostgreSql(PostgreSqlFixture f) => new()
    {
        Register = (s, setup) =>
        {
            s.AddScoped<IDbConnection>(_ => f.CreateConnection());
            global::Encina.ADO.PostgreSQL.ServiceCollectionExtensions.AddEncinaADO(s, c => Configure(c, setup));
        },
        ReadRow = id => ToRowAsync(new global::Encina.ADO.PostgreSQL.Inbox.InboxStoreADO(f.CreateConnection()).GetMessageAsync(id)),
        BreakTransaction = CloseScopedConnection
    };

    /// <summary>ADO.NET over MySQL.</summary>
    public static Harness AdoMySql(MySqlFixture f) => new()
    {
        Register = (s, setup) =>
        {
            s.AddScoped<IDbConnection>(_ => f.CreateConnection());
            global::Encina.ADO.MySQL.ServiceCollectionExtensions.AddEncinaADO(s, c => Configure(c, setup));
        },
        ReadRow = id => ToRowAsync(new global::Encina.ADO.MySQL.Inbox.InboxStoreADO(f.CreateConnection()).GetMessageAsync(id)),
        BreakTransaction = CloseScopedConnection
    };

    /// <summary>Dapper over SQL Server.</summary>
    public static Harness DapperSqlServer(SqlServerFixture f) => new()
    {
        Register = (s, setup) =>
        {
            s.AddScoped<IDbConnection>(_ => f.CreateConnection());
            global::Encina.Dapper.SqlServer.ServiceCollectionExtensions.AddEncinaDapper(s, c => Configure(c, setup));
        },
        ReadRow = id => ToRowAsync(new global::Encina.Dapper.SqlServer.Inbox.InboxStoreDapper(f.CreateConnection()).GetMessageAsync(id)),
        BreakTransaction = CloseScopedConnection
    };

    /// <summary>Dapper over PostgreSQL.</summary>
    public static Harness DapperPostgreSql(PostgreSqlFixture f) => new()
    {
        Register = (s, setup) =>
        {
            s.AddScoped<IDbConnection>(_ => f.CreateConnection());
            global::Encina.Dapper.PostgreSQL.ServiceCollectionExtensions.AddEncinaDapper(s, c => Configure(c, setup));
        },
        ReadRow = id => ToRowAsync(new global::Encina.Dapper.PostgreSQL.Inbox.InboxStoreDapper(f.CreateConnection()).GetMessageAsync(id)),
        BreakTransaction = CloseScopedConnection
    };

    /// <summary>Dapper over MySQL.</summary>
    public static Harness DapperMySql(MySqlFixture f) => new()
    {
        Register = (s, setup) =>
        {
            s.AddScoped<IDbConnection>(_ => f.CreateConnection());
            global::Encina.Dapper.MySQL.ServiceCollectionExtensions.AddEncinaDapper(s, c => Configure(c, setup));
        },
        ReadRow = id => ToRowAsync(new global::Encina.Dapper.MySQL.Inbox.InboxStoreDapper(f.CreateConnection()).GetMessageAsync(id)),
        BreakTransaction = CloseScopedConnection
    };

    /// <summary>EF Core over SQL Server.</summary>
    public static Harness EfSqlServer<TContext>(EFCoreSqlServerFixture f)
        where TContext : DbContext => Ef<TContext>(f.ConnectionString, (o, cs) => o.UseSqlServer(cs), () => f.CreateDbContext<TContext>());

    /// <summary>EF Core over PostgreSQL.</summary>
    public static Harness EfPostgreSql<TContext>(EFCorePostgreSqlFixture f)
        where TContext : DbContext => Ef<TContext>(f.ConnectionString, (o, cs) => o.UseNpgsql(cs), () => f.CreateDbContext<TContext>());

    /// <summary>EF Core over a provider configured with <paramref name="useDatabase"/>.</summary>
    public static Harness Ef<TContext>(
        string connectionString,
        Action<DbContextOptionsBuilder, string> useDatabase,
        Func<TContext> createVerifyContext)
        where TContext : DbContext => new()
    {
        Register = (s, setup) =>
        {
            s.AddDbContext<TContext>(o => useDatabase(o, connectionString));
            s.AddEncinaEntityFrameworkCore<TContext>(c => Configure(c, setup));
        },
        ReadRow = async id =>
        {
            await using var verify = createVerifyContext();
            var row = await verify.Set<global::Encina.EntityFrameworkCore.Inbox.InboxMessage>()
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.MessageId == id);
            return row is null ? null : new InboxRow(row.RetryCount, row.IsProcessed, row.Response);
        },
        BreakTransaction = scope =>
        {
            scope.GetRequiredService<TContext>().Database.GetDbConnection().Close();
            return Task.CompletedTask;
        }
    };

    /// <summary>MongoDB (no business transaction in the pipeline).</summary>
    public static Harness Mongo(MongoDbFixture f)
    {
        var options = Options.Create(new global::Encina.MongoDB.EncinaMongoDbOptions
        {
            DatabaseName = MongoDbFixture.DatabaseName,
            UseInbox = true
        });

        return new Harness
        {
            HasBusinessTransaction = false,
            Register = (s, setup) =>
                global::Encina.MongoDB.ServiceCollectionExtensions.AddEncinaMongoDB(s, f.Client!, o =>
                {
                    o.DatabaseName = MongoDbFixture.DatabaseName;
                    o.UseInbox = true;
                    o.InboxOptions.MaxRetries = setup.MaxRetries;
                }),
            ReadRow = id => ToRowAsync(new global::Encina.MongoDB.Inbox.InboxStoreMongoDB(
                f.Client!, options, NullLogger<global::Encina.MongoDB.Inbox.InboxStoreMongoDB>.Instance).GetMessageAsync(id))
        };
    }

    private static void Configure(MessagingConfiguration config, Setup setup)
    {
        // The provider's own registration decides the behavior order, as in a real application.
        config.UseInbox = true;
        config.UseTransactions = setup.Transactions;
        config.InboxOptions.MaxRetries = setup.MaxRetries;
    }
}
