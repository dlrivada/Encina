using System.Data;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Migrations;
using Encina.Sharding.ReferenceTables;
using Encina.Sharding.ReplicaSelection;
using LanguageExt;
using NSubstitute;
using Shouldly;
using Xunit;
using MySqlMigrations = Encina.ADO.MySQL.Sharding.Migrations;
using MySqlRef = Encina.ADO.MySQL.Sharding.ReferenceTables;
using MySqlRepo = Encina.ADO.MySQL.Repository;
using MySqlRw = Encina.ADO.MySQL.Sharding;
using MySqlSoftDelete = Encina.ADO.MySQL.SoftDelete;
using MySqlTenancy = Encina.ADO.MySQL.Tenancy;
using NpgMigrations = Encina.ADO.PostgreSQL.Sharding.Migrations;
using NpgRef = Encina.ADO.PostgreSQL.Sharding.ReferenceTables;
using NpgRepo = Encina.ADO.PostgreSQL.Repository;
using NpgRw = Encina.ADO.PostgreSQL.Sharding;
using NpgSoftDelete = Encina.ADO.PostgreSQL.SoftDelete;
using NpgTemporal = Encina.ADO.PostgreSQL.Temporal;
using NpgTenancy = Encina.ADO.PostgreSQL.Tenancy;
using SqlMigrations = Encina.ADO.SqlServer.Sharding.Migrations;
using SqlRef = Encina.ADO.SqlServer.Sharding.ReferenceTables;
using SqlRepo = Encina.ADO.SqlServer.Repository;
using SqlRw = Encina.ADO.SqlServer.Sharding;
using SqlSoftDelete = Encina.ADO.SqlServer.SoftDelete;
using SqlTemporal = Encina.ADO.SqlServer.Temporal;
using SqlTenancy = Encina.ADO.SqlServer.Tenancy;

namespace Encina.UnitTests.ADO;

/// <summary>
/// Runs every reachable <c>EncinaErrors.Create</c> producer of the three ADO.NET provider packages
/// (SqlServer, PostgreSQL, MySQL) and reads the declared category back with <c>GetCategory()</c>
/// (#3728, ADR-062 R1 and R4: wiring, topology, store and provider failures are <c>Internal</c>).
/// One class checks all three providers so the same code answers the same category everywhere.
/// </summary>
/// <remarks>
/// Not reachable through the public API, so only the completeness static scan guards them:
/// <c>MissingColumnMappings</c> in the four builders (<c>HasId</c> always adds a column mapping, and
/// the identifier is checked first).
/// </remarks>
[Trait("Category", "Unit")]
public sealed class AdoErrorCategoryTests
{
    public enum Provider
    {
        SqlServer,
        PostgreSQL,
        MySQL
    }

    #region Helpers

    private static EncinaError LeftOf<T>(Either<EncinaError, T> result)
    {
        result.IsLeft.ShouldBeTrue();
        EncinaError error = default;
        _ = result.IfLeft(e => error = e);
        return error;
    }

    private static void AssertInternal<T>(Either<EncinaError, T> result, string expectedCode)
        => AssertInternal(LeftOf(result), expectedCode);

    private static void AssertInternal(EncinaError error, string expectedCode)
    {
        error.GetCode().IsSome.ShouldBeTrue();
        error.GetCode().IfSome(c => c.ShouldBe(expectedCode));
        error.GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    private static string MappingCode(Provider provider, string suffix) => provider switch
    {
        Provider.SqlServer => $"ado.sqlserver.mapping.{suffix}",
        Provider.PostgreSQL => $"ado.postgresql.mapping.{suffix}",
        _ => $"ado.mysql.mapping.{suffix}"
    };

    private static IShardedConnectionFactory ThrowingConnectionFactory()
    {
        var factory = Substitute.For<IShardedConnectionFactory>();
        factory.GetConnectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<Either<EncinaError, IDbConnection>>>(_ => throw new InvalidOperationException("boom"));
        return factory;
    }

    private static ShardInfo Shard(string id = "shard-1") => new(id, "bogus-primary");

    private sealed class Widget
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? TenantId { get; set; }

        public bool IsDeleted { get; set; }
    }

    private sealed class ReferenceRow
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    #endregion

    #region Entity mapping builders

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public void RepositoryBuilder_NoTable_IsMissingTableNameInternal(Provider provider)
    {
        var error = provider switch
        {
            Provider.SqlServer => LeftOf(new SqlRepo.EntityMappingBuilder<Widget, Guid>().Build()),
            Provider.PostgreSQL => LeftOf(new NpgRepo.EntityMappingBuilder<Widget, Guid>().Build()),
            _ => LeftOf(new MySqlRepo.EntityMappingBuilder<Widget, Guid>().Build())
        };

        AssertInternal(error, MappingCode(provider, "missing_table_name"));
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public void RepositoryBuilder_NoPrimaryKey_IsMissingPrimaryKeyInternal(Provider provider)
    {
        var error = provider switch
        {
            Provider.SqlServer => LeftOf(new SqlRepo.EntityMappingBuilder<Widget, Guid>().ToTable("Widgets").Build()),
            Provider.PostgreSQL => LeftOf(new NpgRepo.EntityMappingBuilder<Widget, Guid>().ToTable("Widgets").Build()),
            _ => LeftOf(new MySqlRepo.EntityMappingBuilder<Widget, Guid>().ToTable("Widgets").Build())
        };

        AssertInternal(error, MappingCode(provider, "missing_primary_key"));
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public void SoftDeleteBuilder_NoTable_IsMissingTableNameInternal(Provider provider)
    {
        var error = provider switch
        {
            Provider.SqlServer => LeftOf(new SqlSoftDelete.SoftDeleteEntityMappingBuilder<Widget, Guid>().Build()),
            Provider.PostgreSQL => LeftOf(new NpgSoftDelete.SoftDeleteEntityMappingBuilder<Widget, Guid>().Build()),
            _ => LeftOf(new MySqlSoftDelete.SoftDeleteEntityMappingBuilder<Widget, Guid>().Build())
        };

        AssertInternal(error, MappingCode(provider, "missing_table_name"));
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public void SoftDeleteBuilder_NoPrimaryKey_IsMissingPrimaryKeyInternal(Provider provider)
    {
        var error = provider switch
        {
            Provider.SqlServer => LeftOf(new SqlSoftDelete.SoftDeleteEntityMappingBuilder<Widget, Guid>().ToTable("Widgets").Build()),
            Provider.PostgreSQL => LeftOf(new NpgSoftDelete.SoftDeleteEntityMappingBuilder<Widget, Guid>().ToTable("Widgets").Build()),
            _ => LeftOf(new MySqlSoftDelete.SoftDeleteEntityMappingBuilder<Widget, Guid>().ToTable("Widgets").Build())
        };

        AssertInternal(error, MappingCode(provider, "missing_primary_key"));
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public void TenantBuilder_NoTable_IsMissingTableNameInternal(Provider provider)
    {
        var error = provider switch
        {
            Provider.SqlServer => LeftOf(new SqlTenancy.TenantEntityMappingBuilder<Widget, Guid>().Build()),
            Provider.PostgreSQL => LeftOf(new NpgTenancy.TenantEntityMappingBuilder<Widget, Guid>().Build()),
            _ => LeftOf(new MySqlTenancy.TenantEntityMappingBuilder<Widget, Guid>().Build())
        };

        AssertInternal(error, MappingCode(provider, "missing_table_name"));
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public void TenantBuilder_NoPrimaryKey_IsMissingPrimaryKeyInternal(Provider provider)
    {
        var error = provider switch
        {
            Provider.SqlServer => LeftOf(new SqlTenancy.TenantEntityMappingBuilder<Widget, Guid>().ToTable("Widgets").Build()),
            Provider.PostgreSQL => LeftOf(new NpgTenancy.TenantEntityMappingBuilder<Widget, Guid>().ToTable("Widgets").Build()),
            _ => LeftOf(new MySqlTenancy.TenantEntityMappingBuilder<Widget, Guid>().ToTable("Widgets").Build())
        };

        AssertInternal(error, MappingCode(provider, "missing_primary_key"));
    }

    // MySQL has no temporal builder (a missing feature, not a category divergence).
    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    public void TemporalBuilder_NoPrimaryKey_IsMissingPrimaryKeyInternal(Provider provider)
    {
        var error = provider == Provider.SqlServer
            ? LeftOf(new SqlTemporal.TemporalEntityMappingBuilder<Widget, Guid>().Build())
            : LeftOf(new NpgTemporal.TemporalEntityMappingBuilder<Widget, Guid>().Build());

        AssertInternal(error, MappingCode(provider, "missing_primary_key"));
    }

    #endregion

    #region Migration executor, history store and schema introspector

    private static IMigrationExecutor CreateExecutor(Provider provider, IShardedConnectionFactory factory) => provider switch
    {
        Provider.SqlServer => new SqlMigrations.AdoMigrationExecutor(factory),
        Provider.PostgreSQL => new NpgMigrations.AdoMigrationExecutor(factory),
        _ => new MySqlMigrations.AdoMigrationExecutor(factory)
    };

    private static IMigrationHistoryStore CreateHistoryStore(Provider provider, IShardedConnectionFactory factory) => provider switch
    {
        Provider.SqlServer => new SqlMigrations.AdoMigrationHistoryStore(factory),
        Provider.PostgreSQL => new NpgMigrations.AdoMigrationHistoryStore(factory),
        _ => new MySqlMigrations.AdoMigrationHistoryStore(factory)
    };

    private static ISchemaIntrospector CreateIntrospector(Provider provider, IShardedConnectionFactory factory) => provider switch
    {
        Provider.SqlServer => new SqlMigrations.SqlServerSchemaIntrospector(factory),
        Provider.PostgreSQL => new NpgMigrations.PostgreSqlSchemaIntrospector(factory),
        _ => new MySqlMigrations.MySqlSchemaIntrospector(factory)
    };

    private static MigrationScript Script() => new("m1", "CREATE TABLE t (id INT)", "DROP TABLE t", "desc", "checksum");

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task Executor_ExecuteSqlThrows_IsMigrationFailedInternal(Provider provider)
    {
        var executor = CreateExecutor(provider, ThrowingConnectionFactory());

        var result = await executor.ExecuteSqlAsync(Shard(), "SELECT 1");

        AssertInternal(result, MigrationErrorCodes.MigrationFailed);
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task HistoryStore_GetAppliedThrows_IsMigrationFailedInternal(Provider provider)
    {
        var store = CreateHistoryStore(provider, ThrowingConnectionFactory());

        var result = await store.GetAppliedAsync("shard-1");

        AssertInternal(result, MigrationErrorCodes.MigrationFailed);
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task HistoryStore_RecordAppliedThrows_IsMigrationFailedInternal(Provider provider)
    {
        var store = CreateHistoryStore(provider, ThrowingConnectionFactory());

        var result = await store.RecordAppliedAsync("shard-1", Script(), TimeSpan.FromSeconds(1));

        AssertInternal(result, MigrationErrorCodes.MigrationFailed);
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task HistoryStore_RecordRolledBackThrows_IsRollbackFailedInternal(Provider provider)
    {
        var store = CreateHistoryStore(provider, ThrowingConnectionFactory());

        var result = await store.RecordRolledBackAsync("shard-1", "m1");

        AssertInternal(result, MigrationErrorCodes.RollbackFailed);
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task HistoryStore_EnsureHistoryTableThrows_IsMigrationFailedInternal(Provider provider)
    {
        var store = CreateHistoryStore(provider, ThrowingConnectionFactory());

        var result = await store.EnsureHistoryTableExistsAsync(Shard());

        AssertInternal(result, MigrationErrorCodes.MigrationFailed);
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task HistoryStore_ApplyHistoricalThrows_IsMigrationFailedInternal(Provider provider)
    {
        var store = CreateHistoryStore(provider, ThrowingConnectionFactory());

        var result = await store.ApplyHistoricalMigrationsAsync("shard-1", [Script()]);

        AssertInternal(result, MigrationErrorCodes.MigrationFailed);
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task Introspector_CompareThrows_IsSchemaComparisonFailedInternal(Provider provider)
    {
        var introspector = CreateIntrospector(provider, ThrowingConnectionFactory());

        var result = await introspector.CompareAsync(Shard("a"), Shard("b"), includeColumnDiffs: false);

        AssertInternal(result, MigrationErrorCodes.SchemaComparisonFailed);
    }

    #endregion

    #region Reference table store

    private static IReferenceTableStore CreateReferenceStore(Provider provider, IDbConnection connection) => provider switch
    {
        Provider.SqlServer => new SqlRef.ReferenceTableStoreADO(connection),
        Provider.PostgreSQL => new NpgRef.ReferenceTableStoreADO(connection),
        _ => new MySqlRef.ReferenceTableStoreADO(connection)
    };

    private static IDbConnection ThrowingConnection()
    {
        var connection = Substitute.For<IDbConnection>();
        connection.CreateCommand().Returns(_ => throw new InvalidOperationException("boom"));
        return connection;
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task ReferenceStore_GetAllThrows_IsGetAllFailedInternal(Provider provider)
    {
        using var store = (IDisposable)CreateReferenceStore(provider, ThrowingConnection());

        var result = await ((IReferenceTableStore)store).GetAllAsync<Widget>();

        AssertInternal(result, ReferenceTableErrorCodes.GetAllFailed);
    }

    // Success path through the synchronous IDbCommand fallback (the connection is not a DbConnection),
    // so the category change leaves the working path of the store covered next to its error path.
    [Theory]
    [InlineData(Provider.SqlServer, true)]
    [InlineData(Provider.SqlServer, false)]
    [InlineData(Provider.PostgreSQL, true)]
    [InlineData(Provider.PostgreSQL, false)]
    [InlineData(Provider.MySQL, true)]
    [InlineData(Provider.MySQL, false)]
    public async Task ReferenceStore_GetAllWithoutDbCommand_ReturnsMappedRows(Provider provider, bool hasRow)
    {
        var id = Guid.NewGuid();
        var reader = Substitute.For<IDataReader>();
        reader.Read().Returns(hasRow, false);
        reader.GetValue(Arg.Any<int>()).Returns(call => call.Arg<int>() == 0 ? id : (object)"w");
        var command = Substitute.For<IDbCommand>();
        command.ExecuteReader().Returns(reader);
        var connection = Substitute.For<IDbConnection>();
        connection.CreateCommand().Returns(command);
        using var store = (IDisposable)CreateReferenceStore(provider, connection);

        var result = await ((IReferenceTableStore)store).GetAllAsync<ReferenceRow>();

        result.IsRight.ShouldBeTrue();
        result.IfRight(rows => rows.Count.ShouldBe(hasRow ? 1 : 0));
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task ReferenceStore_UpsertThrows_IsUpsertFailedInternal(Provider provider)
    {
        using var store = (IDisposable)CreateReferenceStore(provider, ThrowingConnection());

        var result = await ((IReferenceTableStore)store)
            .UpsertAsync([new Widget { Id = Guid.NewGuid(), Name = "w" }]);

        AssertInternal(result, ReferenceTableErrorCodes.UpsertFailed);
    }

    #endregion

    #region Read/write connection factory replica selection

    private static IShardedReadWriteConnectionFactory CreateReadWriteFactory(
        Provider provider,
        ShardTopology topology,
        bool fallbackToPrimary,
        IReplicaHealthTracker tracker)
    {
        var options = new ShardedReadWriteOptions { FallbackToPrimaryWhenNoReplicas = fallbackToPrimary };
        return provider switch
        {
            Provider.SqlServer => new SqlRw.ShardedReadWriteConnectionFactory(topology, options, tracker),
            Provider.PostgreSQL => new NpgRw.ShardedReadWriteConnectionFactory(topology, options, tracker),
            _ => new MySqlRw.ShardedReadWriteConnectionFactory(topology, options, tracker)
        };
    }

    private static ShardTopology Topology(ShardInfo shard) => new([shard]);

    private static IReplicaHealthTracker Tracker(params string[] available)
    {
        var tracker = Substitute.For<IReplicaHealthTracker>();
        tracker.GetAvailableReplicas(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(available);
        return tracker;
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task Factory_NoReplicasWithoutFallback_IsNoReplicasConfiguredInternal(Provider provider)
    {
        var factory = CreateReadWriteFactory(provider, Topology(Shard()), fallbackToPrimary: false, Tracker());

        var result = await factory.GetReadConnectionAsync("shard-1");

        AssertInternal(result, ShardingErrorCodes.NoReplicasConfigured);
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task Factory_AllReplicasUnhealthyWithoutFallback_IsAllReplicasUnhealthyInternal(Provider provider)
    {
        var shard = new ShardInfo("shard-1", "bogus-primary", ReplicaConnectionStrings: ["bogus-replica"]);
        var factory = CreateReadWriteFactory(provider, Topology(shard), fallbackToPrimary: false, Tracker());

        var result = await factory.GetReadConnectionAsync("shard-1");

        AssertInternal(result, ShardingErrorCodes.AllReplicasUnhealthy);
    }

    // The three paths below select a connection string and then construct the provider connection,
    // which rejects the malformed string at once: the replica selection lines run to the end.
    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task Factory_NoReplicasWithFallback_SelectsPrimary(Provider provider)
    {
        var factory = CreateReadWriteFactory(provider, Topology(Shard()), fallbackToPrimary: true, Tracker());

        await Should.ThrowAsync<ArgumentException>(async () => await factory.GetReadConnectionAsync("shard-1"));
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task Factory_AllReplicasUnhealthyWithFallback_SelectsPrimary(Provider provider)
    {
        var shard = new ShardInfo("shard-1", "bogus-primary", ReplicaConnectionStrings: ["bogus-replica"]);
        var factory = CreateReadWriteFactory(provider, Topology(shard), fallbackToPrimary: true, Tracker());

        await Should.ThrowAsync<ArgumentException>(async () => await factory.GetReadConnectionAsync("shard-1"));
    }

    [Theory]
    [InlineData(Provider.SqlServer, false)]
    [InlineData(Provider.SqlServer, true)]
    [InlineData(Provider.PostgreSQL, false)]
    [InlineData(Provider.PostgreSQL, true)]
    [InlineData(Provider.MySQL, false)]
    [InlineData(Provider.MySQL, true)]
    public async Task Factory_HealthyReplica_SelectsReplica(Provider provider, bool shardOverridesStrategy)
    {
        var shard = new ShardInfo(
            "shard-1",
            "bogus-primary",
            ReplicaConnectionStrings: ["bogus-replica"],
            ReplicaStrategy: shardOverridesStrategy ? ReplicaSelectionStrategy.Random : null);
        var factory = CreateReadWriteFactory(provider, Topology(shard), fallbackToPrimary: false, Tracker("bogus-replica"));

        await Should.ThrowAsync<ArgumentException>(async () => await factory.GetReadConnectionAsync("shard-1"));
    }

    #endregion
}
