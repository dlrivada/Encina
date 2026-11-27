using System.Data;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.ReferenceTables;
using Encina.Sharding.ReplicaSelection;
using LanguageExt;
using NSubstitute;
using Shouldly;
using Xunit;
using MySqlRef = Encina.Dapper.MySQL.Sharding.ReferenceTables;
using MySqlRepo = Encina.Dapper.MySQL.Repository;
using MySqlRw = Encina.Dapper.MySQL.Sharding;
using MySqlSoftDelete = Encina.Dapper.MySQL.SoftDelete;
using MySqlTenancy = Encina.Dapper.MySQL.Tenancy;
using NpgRef = Encina.Dapper.PostgreSQL.Sharding.ReferenceTables;
using NpgRepo = Encina.Dapper.PostgreSQL.Repository;
using NpgRw = Encina.Dapper.PostgreSQL.Sharding;
using NpgSoftDelete = Encina.Dapper.PostgreSQL.SoftDelete;
using NpgTemporal = Encina.Dapper.PostgreSQL.Temporal;
using NpgTenancy = Encina.Dapper.PostgreSQL.Tenancy;
using SqlRef = Encina.Dapper.SqlServer.Sharding.ReferenceTables;
using SqlRepo = Encina.Dapper.SqlServer.Repository;
using SqlRw = Encina.Dapper.SqlServer.Sharding;
using SqlSoftDelete = Encina.Dapper.SqlServer.SoftDelete;
using SqlTemporal = Encina.Dapper.SqlServer.Temporal;
using SqlTenancy = Encina.Dapper.SqlServer.Tenancy;

namespace Encina.UnitTests.Dapper;

/// <summary>
/// Runs every reachable <c>EncinaErrors.Create</c> producer of the three Dapper provider packages
/// (SqlServer, PostgreSQL, MySQL) and reads the declared category back with <c>GetCategory()</c>
/// (#3729, ADR-062 R1 and R4: wiring, topology and store failures are <c>Internal</c>; D11 of
/// 2026-11-25: <c>AllReplicasUnhealthy</c> is <c>Unavailable</c>). One class checks all three
/// providers so the same code answers the same category everywhere.
/// </summary>
/// <remarks>
/// Not reachable through the public API, so only the completeness static scan guards them:
/// <c>MissingColumnMappings</c> in the builders (<c>HasId</c> always adds a column mapping, and the
/// identifier is checked first).
/// </remarks>
[Trait("Category", "Unit")]
public sealed class DapperErrorCategoryTests
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

    private static T RightOf<T>(Either<EncinaError, T> result)
    {
        result.IsRight.ShouldBeTrue();
        T value = default!;
        _ = result.IfRight(v => value = v);
        return value;
    }

    private static void AssertCategory<T>(Either<EncinaError, T> result, string expectedCode, ErrorCategory expected = ErrorCategory.Internal)
        => AssertCategory(LeftOf(result), expectedCode, expected);

    private static void AssertCategory(EncinaError error, string expectedCode, ErrorCategory expected = ErrorCategory.Internal)
    {
        error.GetCode().IsSome.ShouldBeTrue();
        error.GetCode().IfSome(c => c.ShouldBe(expectedCode));
        error.GetCategory().ShouldBe(expected);
    }

    private static string MappingCode(Provider provider, string suffix) => provider switch
    {
        Provider.SqlServer => $"dapper.sqlserver.mapping.{suffix}",
        Provider.PostgreSQL => $"dapper.postgresql.mapping.{suffix}",
        _ => $"dapper.mysql.mapping.{suffix}"
    };

    private static ShardInfo Shard(string id = "shard-1") => new(id, "bogus-primary");

    private sealed class Widget
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? TenantId { get; set; }

        public bool IsDeleted { get; set; }
    }

    #endregion

    #region Entity mapping builders: error paths

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

        AssertCategory(error, MappingCode(provider, "missing_table_name"));
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

        AssertCategory(error, MappingCode(provider, "missing_primary_key"));
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

        AssertCategory(error, MappingCode(provider, "missing_table_name"));
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

        AssertCategory(error, MappingCode(provider, "missing_primary_key"));
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

        AssertCategory(error, MappingCode(provider, "missing_table_name"));
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

        AssertCategory(error, MappingCode(provider, "missing_primary_key"));
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

        AssertCategory(error, MappingCode(provider, "missing_primary_key"));
    }

    #endregion

    #region Entity mapping builders: success path

    private static void AssertMapping(string tableName, string idColumnName, IReadOnlyDictionary<string, string> columns, Guid expectedId, Guid actualId)
    {
        tableName.ShouldBe("Widgets");
        idColumnName.ShouldBe("Id");
        columns["Id"].ShouldBe("Id");
        columns["Name"].ShouldBe("WidgetName");
        actualId.ShouldBe(expectedId);
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public void RepositoryBuilder_Configured_BuildsMapping(Provider provider)
    {
        var widget = new Widget { Id = Guid.NewGuid() };

        switch (provider)
        {
            case Provider.SqlServer:
                var sql = RightOf(new SqlRepo.EntityMappingBuilder<Widget, Guid>()
                    .ToTable("Widgets").HasId(w => w.Id).MapProperty(w => w.Name, "WidgetName").Build());
                AssertMapping(sql.TableName, sql.IdColumnName, sql.ColumnMappings, widget.Id, sql.GetId(widget));
                break;
            case Provider.PostgreSQL:
                var npg = RightOf(new NpgRepo.EntityMappingBuilder<Widget, Guid>()
                    .ToTable("Widgets").HasId(w => w.Id).MapProperty(w => w.Name, "WidgetName").Build());
                AssertMapping(npg.TableName, npg.IdColumnName, npg.ColumnMappings, widget.Id, npg.GetId(widget));
                break;
            default:
                var my = RightOf(new MySqlRepo.EntityMappingBuilder<Widget, Guid>()
                    .ToTable("Widgets").HasId(w => w.Id).MapProperty(w => w.Name, "WidgetName").Build());
                AssertMapping(my.TableName, my.IdColumnName, my.ColumnMappings, widget.Id, my.GetId(widget));
                break;
        }
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public void SoftDeleteBuilder_Configured_BuildsMapping(Provider provider)
    {
        var widget = new Widget { Id = Guid.NewGuid() };

        switch (provider)
        {
            case Provider.SqlServer:
                var sql = RightOf(new SqlSoftDelete.SoftDeleteEntityMappingBuilder<Widget, Guid>()
                    .ToTable("Widgets").HasId(w => w.Id).MapProperty(w => w.Name, "WidgetName").Build());
                AssertMapping(sql.TableName, sql.IdColumnName, sql.ColumnMappings, widget.Id, sql.GetId(widget));
                break;
            case Provider.PostgreSQL:
                var npg = RightOf(new NpgSoftDelete.SoftDeleteEntityMappingBuilder<Widget, Guid>()
                    .ToTable("Widgets").HasId(w => w.Id).MapProperty(w => w.Name, "WidgetName").Build());
                AssertMapping(npg.TableName, npg.IdColumnName, npg.ColumnMappings, widget.Id, npg.GetId(widget));
                break;
            default:
                var my = RightOf(new MySqlSoftDelete.SoftDeleteEntityMappingBuilder<Widget, Guid>()
                    .ToTable("Widgets").HasId(w => w.Id).MapProperty(w => w.Name, "WidgetName").Build());
                AssertMapping(my.TableName, my.IdColumnName, my.ColumnMappings, widget.Id, my.GetId(widget));
                break;
        }
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public void TenantBuilder_Configured_BuildsMapping(Provider provider)
    {
        var widget = new Widget { Id = Guid.NewGuid() };

        switch (provider)
        {
            case Provider.SqlServer:
                var sql = RightOf(new SqlTenancy.TenantEntityMappingBuilder<Widget, Guid>()
                    .ToTable("Widgets").HasId(w => w.Id).MapProperty(w => w.Name, "WidgetName").Build());
                AssertMapping(sql.TableName, sql.IdColumnName, sql.ColumnMappings, widget.Id, sql.GetId(widget));
                break;
            case Provider.PostgreSQL:
                var npg = RightOf(new NpgTenancy.TenantEntityMappingBuilder<Widget, Guid>()
                    .ToTable("Widgets").HasId(w => w.Id).MapProperty(w => w.Name, "WidgetName").Build());
                AssertMapping(npg.TableName, npg.IdColumnName, npg.ColumnMappings, widget.Id, npg.GetId(widget));
                break;
            default:
                var my = RightOf(new MySqlTenancy.TenantEntityMappingBuilder<Widget, Guid>()
                    .ToTable("Widgets").HasId(w => w.Id).MapProperty(w => w.Name, "WidgetName").Build());
                AssertMapping(my.TableName, my.IdColumnName, my.ColumnMappings, widget.Id, my.GetId(widget));
                break;
        }
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    public void TemporalBuilder_Configured_BuildsMapping(Provider provider)
    {
        var widget = new Widget { Id = Guid.NewGuid() };

        if (provider == Provider.SqlServer)
        {
            var sql = RightOf(new SqlTemporal.TemporalEntityMappingBuilder<Widget, Guid>()
                .ToTable("Widgets").HasId(w => w.Id, "Id").MapProperty(w => w.Name, "WidgetName").Build());
            AssertMapping(sql.TableName, sql.IdColumnName, sql.ColumnMappings, widget.Id, sql.GetId(widget));
        }
        else
        {
            var npg = RightOf(new NpgTemporal.TemporalEntityMappingBuilder<Widget, Guid>()
                .ToTable("Widgets").HasId(w => w.Id, "Id").MapProperty(w => w.Name, "WidgetName").Build());
            AssertMapping(npg.TableName, npg.IdColumnName, npg.ColumnMappings, widget.Id, npg.GetId(widget));
        }
    }

    #endregion

    #region Reference table store

    private static IReferenceTableStore CreateReferenceStore(Provider provider, IDbConnection connection) => provider switch
    {
        Provider.SqlServer => new SqlRef.ReferenceTableStoreDapper(connection),
        Provider.PostgreSQL => new NpgRef.ReferenceTableStoreDapper(connection),
        _ => new MySqlRef.ReferenceTableStoreDapper(connection)
    };

    // Dapper's async API rejects a connection whose commands are not DbCommand, so the call throws
    // inside the store's try block and the catch produces the categorized error.
    private static IDbConnection NonDbConnection() => Substitute.For<IDbConnection>();

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task ReferenceStore_GetAllThrows_IsGetAllFailedInternal(Provider provider)
    {
        using var store = (IDisposable)CreateReferenceStore(provider, NonDbConnection());

        var result = await ((IReferenceTableStore)store).GetAllAsync<Widget>();

        AssertCategory(result, ReferenceTableErrorCodes.GetAllFailed);
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task ReferenceStore_UpsertThrows_IsUpsertFailedInternal(Provider provider)
    {
        using var store = (IDisposable)CreateReferenceStore(provider, NonDbConnection());

        var result = await ((IReferenceTableStore)store)
            .UpsertAsync([new Widget { Id = Guid.NewGuid(), Name = "w" }]);

        AssertCategory(result, ReferenceTableErrorCodes.UpsertFailed);
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task ReferenceStore_UpsertEmpty_ReturnsZero(Provider provider)
    {
        using var store = (IDisposable)CreateReferenceStore(provider, NonDbConnection());

        var result = await ((IReferenceTableStore)store).UpsertAsync(Array.Empty<Widget>());

        RightOf(result).ShouldBe(0);
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

        AssertCategory(result, ShardingErrorCodes.NoReplicasConfigured);
    }

    [Theory]
    [InlineData(Provider.SqlServer)]
    [InlineData(Provider.PostgreSQL)]
    [InlineData(Provider.MySQL)]
    public async Task Factory_AllReplicasUnhealthyWithoutFallback_IsAllReplicasUnhealthyUnavailable(Provider provider)
    {
        var shard = new ShardInfo("shard-1", "bogus-primary", ReplicaConnectionStrings: ["bogus-replica"]);
        var factory = CreateReadWriteFactory(provider, Topology(shard), fallbackToPrimary: false, Tracker());

        var result = await factory.GetReadConnectionAsync("shard-1");

        // D11 (decided 2026-11-25): a transient replica outage answers 503, not 500.
        AssertCategory(result, ShardingErrorCodes.AllReplicasUnhealthy, ErrorCategory.Unavailable);
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
