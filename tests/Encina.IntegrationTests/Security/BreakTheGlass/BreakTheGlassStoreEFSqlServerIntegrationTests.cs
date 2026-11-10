using System.Transactions;
using Encina.ContractTests.Security.BreakTheGlass;
using Encina.EntityFrameworkCore.BreakTheGlass;
using Encina.IntegrationTests.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Encina.IntegrationTests.Security.BreakTheGlass;

/// <summary>A context with only the break-the-glass model, for the EF Core store tests.</summary>
public sealed class BreakTheGlassTestDbContext(DbContextOptions<BreakTheGlassTestDbContext> options) : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyBreakTheGlassConfiguration();
}

/// <summary>Creates a context on the shared EF Core SQL Server fixture for every call, like AddDbContextFactory does.</summary>
internal sealed class BreakTheGlassTestContextFactory(EFCoreSqlServerFixture fixture) : IDbContextFactory<BreakTheGlassTestDbContext>
{
    public BreakTheGlassTestDbContext CreateDbContext() => fixture.CreateDbContext<BreakTheGlassTestDbContext>();
}

/// <summary>Creates the table from the script the SQL Server packages ship, and clears it.</summary>
internal static class BreakTheGlassEFSchema
{
    public static async Task PrepareAsync(EFCoreSqlServerFixture fixture)
    {
        await fixture.ClearAllDataAsync();
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        foreach (var batch in ShippedSqlScript.SplitBatches(ShippedSqlScript.Read("Encina.ADO.SqlServer", BreakTheGlassSqlServerSchema.Script)))
        {
            await using var create = new SqlCommand(batch, connection);
            await create.ExecuteNonQueryAsync();
        }

        await using var clear = new SqlCommand("DELETE FROM BreakTheGlassElevations;", connection);
        await clear.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// The shared <see cref="BreakTheGlassStoreContract"/> against <see cref="BreakTheGlassStoreEF{TContext}"/> on SQL Server.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class BreakTheGlassStoreEFSqlServerIntegrationTests : BreakTheGlassStoreContract
{
    private readonly EFCoreSqlServerFixture _fixture;

    public BreakTheGlassStoreEFSqlServerIntegrationTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    protected override async Task<IBreakTheGlassStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        await BreakTheGlassEFSchema.PrepareAsync(_fixture);
        return CreateSecondStore();
    }

    protected override IBreakTheGlassStore CreateSecondStore() =>
        new BreakTheGlassStoreEF<BreakTheGlassTestDbContext>(new BreakTheGlassTestContextFactory(_fixture));
}

/// <summary>Behavior of <see cref="BreakTheGlassStoreEF{TContext}"/> that the shared contract does not cover.</summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class BreakTheGlassStoreEFSqlServerEdgeIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Opened = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly EFCoreSqlServerFixture _fixture;
    private BreakTheGlassStoreEF<BreakTheGlassTestDbContext> _store = null!;

    public BreakTheGlassStoreEFSqlServerEdgeIntegrationTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await BreakTheGlassEFSchema.PrepareAsync(_fixture);
        _store = new BreakTheGlassStoreEF<BreakTheGlassTestDbContext>(new BreakTheGlassTestContextFactory(_fixture));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static BreakTheGlassElevation Elevation(string? tenant, string user = "clin-1", string subject = "patient-1") => new()
    {
        ElevationId = Guid.NewGuid(),
        TenantId = tenant,
        UserId = user,
        SubjectId = subject,
        Scope = "clinical-record",
        Justification = "justification",
        OpenedAtUtc = Opened,
        ExpiresAtUtc = Opened.AddHours(1)
    };

    private static string CodeOf<T>(Either<EncinaError, T> result) =>
        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty));

    [Fact]
    public async Task ACloseInsideARolledBackTransactionScope_StillPersists()
    {
        var elevation = Elevation("acme");
        (await _store.AddAsync(elevation)).IsRight.ShouldBeTrue();

        using (var scope = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
        {
            var closed = await _store.TryCloseAsync("acme", elevation.ElevationId, BreakTheGlassStatus.Revoked, "revoked", Opened.AddMinutes(1));
            closed.Match(Right: c => c, Left: _ => false).ShouldBeTrue();

            // The scope is disposed without Complete(): the business transaction rolls back.
        }

        var stored = await _store.GetByIdAsync("acme", elevation.ElevationId);
        var status = stored.Match(Right: o => o.Match(Some: e => e.Status, None: () => BreakTheGlassStatus.Active), Left: _ => BreakTheGlassStatus.Active);
        status.ShouldBe(BreakTheGlassStatus.Revoked);
    }

    [Fact]
    public async Task ATenantWithATrailingSpace_IsADifferentTenant()
    {
        var elevation = Elevation("acme");
        (await _store.AddAsync(elevation)).IsRight.ShouldBeTrue();

        var other = await _store.GetByIdAsync("acme ", elevation.ElevationId);
        other.Match(Right: o => o.IsSome, Left: _ => true).ShouldBeFalse();

        var queue = await _store.QueryReviewQueueAsync("acme ", new BreakTheGlassReviewQuery());
        queue.Match(Right: p => p.Items.Count, Left: _ => -1).ShouldBe(0);

        var own = await _store.GetByIdAsync("acme", elevation.ElevationId);
        own.Match(Right: o => o.IsSome, Left: _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task AKeyPartWithTheSeparator_IsRefusedAndNeverCollidesWithAnotherTuple()
    {
        var refused = await _store.AddAsync(Elevation("acme", user: "a\u001fb", subject: "c"));
        CodeOf(refused).ShouldBe(BreakTheGlassErrors.ValidationFailedCode);

        (await _store.AddAsync(Elevation("acme", user: "a", subject: "b"))).IsRight.ShouldBeTrue();

        var found = await _store.FindEffectiveAsync("acme", "a", "b\u001fc", "clinical-record", Opened.AddMinutes(1));
        found.Match(Right: o => o.IsSome, Left: _ => true).ShouldBeFalse();
    }

    [Fact]
    public async Task AHugePageNumber_ReturnsAnEmptyPage()
    {
        (await _store.AddAsync(Elevation("acme"))).IsRight.ShouldBeTrue();

        var page = await _store.QueryReviewQueueAsync("acme", new BreakTheGlassReviewQuery { PageNumber = int.MaxValue, PageSize = BreakTheGlassReviewQuery.MaxPageSize });

        page.IsRight.ShouldBeTrue();
        page.Match(Right: p => p.Items.Count, Left: _ => -1).ShouldBe(0);
    }
}

/// <summary>
/// Proves that the EF Core model matches the script the SQL Server packages ship: the same columns, nullability, lengths,
/// collations, computed persisted <c>ActiveKey</c> and the same indexes with their key columns.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class BreakTheGlassEFSchemaParityIntegrationTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;

    public BreakTheGlassEFSchemaParityIntegrationTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await BreakTheGlassEFSchema.PrepareAsync(_fixture);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private record ColumnInfo(string Name, bool Nullable, int? MaxChars, string? Collation, bool Computed, bool Persisted, string SqlType);

    private static string BaseType(string storeType)
    {
        var paren = storeType.IndexOf('(', StringComparison.Ordinal);
        return (paren < 0 ? storeType : storeType[..paren]).ToLowerInvariant();
    }

    [Fact]
    public async Task EfModelColumns_MatchTheShippedTable()
    {
        await using var context = _fixture.CreateDbContext<BreakTheGlassTestDbContext>();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(BreakTheGlassElevationEntity))!;
        var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());

        var expected = entity.GetProperties()
            .Select(p => new ColumnInfo(
                p.GetColumnName(table)!,
                p.IsColumnNullable(table),
                p.GetMaxLength(),
                p.GetCollation(),
                p.GetComputedColumnSql(table) is not null,
                p.GetIsStored(table) == true,
                BaseType(p.GetRelationalTypeMapping().StoreType)))
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .ToList();

        var actual = await ReadColumnsAsync();

        actual.Select(c => c.Name).ShouldBe(expected.Select(c => c.Name));
        foreach (var column in expected)
        {
            var shipped = actual.Single(c => c.Name == column.Name);
            shipped.Nullable.ShouldBe(column.Nullable, column.Name);
            shipped.Computed.ShouldBe(column.Computed, column.Name);
            shipped.Persisted.ShouldBe(column.Persisted, column.Name);
            shipped.SqlType.ShouldBe(column.SqlType, column.Name);
            if (column.MaxChars is not null)
            {
                shipped.MaxChars.ShouldBe(column.MaxChars, column.Name);
            }

            if (column.Collation is not null)
            {
                shipped.Collation.ShouldBe(column.Collation, column.Name);
            }
        }
    }

    [Fact]
    public async Task EfModelIndexes_MatchTheShippedIndexes()
    {
        await using var context = _fixture.CreateDbContext<BreakTheGlassTestDbContext>();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(BreakTheGlassElevationEntity))!;
        var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());

        var expected = entity.GetIndexes()
            .Select(i => (Name: i.GetDatabaseName()!, i.IsUnique, Columns: string.Join(',', i.Properties.Select(p => p.GetColumnName(table)))))
            .Append((Name: entity.FindPrimaryKey()!.GetName()!, IsUnique: true, Columns: string.Join(',', entity.FindPrimaryKey()!.Properties.Select(p => p.GetColumnName(table)))))
            .OrderBy(i => i.Name, StringComparer.Ordinal)
            .ToList();

        var actual = await ReadIndexesAsync();

        actual.ShouldBe(expected);
    }

    private async Task<List<ColumnInfo>> ReadColumnsAsync()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            @"SELECT c.name, c.is_nullable, c.max_length, t.name, c.collation_name, c.is_computed, ISNULL(cc.is_persisted, 0)
              FROM sys.columns c
              JOIN sys.types t ON t.user_type_id = c.user_type_id
              LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
              WHERE c.object_id = OBJECT_ID(N'[dbo].[BreakTheGlassElevations]')
              ORDER BY c.name COLLATE Latin1_General_BIN2", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new List<ColumnInfo>();
        while (await reader.ReadAsync())
        {
            var bytes = reader.GetInt16(2);
            var isNvarchar = reader.GetString(3) == "nvarchar";
            columns.Add(new ColumnInfo(
                reader.GetString(0),
                reader.GetBoolean(1),
                isNvarchar && bytes > 0 ? bytes / 2 : null,
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetBoolean(5),
                reader.GetBoolean(6),
                reader.GetString(3).ToLowerInvariant()));
        }

        return columns;
    }

    private async Task<List<(string Name, bool IsUnique, string Columns)>> ReadIndexesAsync()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            @"SELECT i.name, i.is_unique, c.name
              FROM sys.indexes i
              JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
              JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
              WHERE i.object_id = OBJECT_ID(N'[dbo].[BreakTheGlassElevations]')
              ORDER BY i.name COLLATE Latin1_General_BIN2, ic.key_ordinal", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var columnsByIndex = new Dictionary<string, (bool IsUnique, List<string> Columns)>();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(0);
            if (!columnsByIndex.TryGetValue(name, out var entry))
            {
                entry = (reader.GetBoolean(1), []);
                columnsByIndex[name] = entry;
            }

            entry.Columns.Add(reader.GetString(2));
        }

        return columnsByIndex
            .Select(pair => (Name: pair.Key, pair.Value.IsUnique, Columns: string.Join(',', pair.Value.Columns)))
            .OrderBy(i => i.Name, StringComparer.Ordinal)
            .ToList();
    }
}
