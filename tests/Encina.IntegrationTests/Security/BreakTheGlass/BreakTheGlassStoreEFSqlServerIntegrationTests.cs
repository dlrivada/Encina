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

    private record ColumnInfo(string Name, bool Nullable, int? MaxChars, string? Collation, bool Computed, bool Persisted);

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
                p.GetIsStored(table) == true))
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
                reader.GetBoolean(6)));
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
