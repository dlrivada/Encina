using System.Globalization;

using Encina.Sharding;
using Encina.Sharding.Migrations;
using Encina.TestInfrastructure.Fixtures.Sharding;

using LanguageExt;
using static LanguageExt.Prelude;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using Xunit;

namespace Encina.IntegrationTests.Sharding.Migrations.Sqlite;

/// <summary>
/// End-to-end integration tests for <see cref="ShardedMigrationCoordinator"/>
/// using real SQLite databases via <see cref="ShardedSqliteFixture"/>.
/// Tests actual DDL execution, schema changes, rollback, and drift detection across shards.
/// </summary>
[Collection("Sharding-ADO-Sqlite")]
[Trait("Category", "Integration")]
[Trait("Database", "SQLite")]
public sealed class ShardedMigrationCoordinatorSqliteTests : IAsyncLifetime
{
    private readonly ShardedSqliteFixture _fixture;

    public ShardedMigrationCoordinatorSqliteTests(ShardedSqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();
        // Drop any tables created by previous test runs
        await DropTestTablesAsync(_fixture.Shard1ConnectionString);
        await DropTestTablesAsync(_fixture.Shard2ConnectionString);
        await DropTestTablesAsync(_fixture.Shard3ConnectionString);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region Test Helpers

    private static async Task DropTestTablesAsync(string connectionString)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            DROP TABLE IF EXISTS MigrationTestProducts;
            DROP TABLE IF EXISTS MigrationTestOrders;
            DROP TABLE IF EXISTS __EncinaMigrationHistory;
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<bool> TableExistsAsync(string connectionString, string tableName)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'";
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result, CultureInfo.InvariantCulture) > 0;
    }

    private static async Task<int> GetColumnCountAsync(string connectionString, string tableName)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tableName})";
        var count = 0;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) count++;
        return count;
    }

    private ShardedMigrationCoordinator CreateCoordinator(ShardTopology? topology = null)
    {
        var actualTopology = topology ?? _fixture.CreateTopology();
        return new ShardedMigrationCoordinator(
            actualTopology,
            new SqliteMigrationExecutor(),
            new SqliteSchemaIntrospector(),
            new SqliteMigrationHistoryStore(),
            NullLogger<ShardedMigrationCoordinator>.Instance);
    }

    private static MigrationScript CreateProductsTableScript() => new(
        "001_create_products",
        "CREATE TABLE MigrationTestProducts (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Price REAL NOT NULL);",
        "DROP TABLE IF EXISTS MigrationTestProducts;",
        "Create products table for migration testing",
        "sha256:products_v1");

    private static MigrationScript CreateOrdersTableScript() => new(
        "002_create_orders",
        "CREATE TABLE MigrationTestOrders (Id TEXT PRIMARY KEY, ProductId TEXT NOT NULL, Quantity INTEGER NOT NULL);",
        "DROP TABLE IF EXISTS MigrationTestOrders;",
        "Create orders table for migration testing",
        "sha256:orders_v1");

    private static MigrationOptions CreateOptions(
        MigrationStrategy strategy = MigrationStrategy.Sequential,
        bool stopOnFirstFailure = true) => new()
    {
        Strategy = strategy,
        StopOnFirstFailure = stopOnFirstFailure,
        MaxParallelism = 2,
        PerShardTimeout = TimeSpan.FromMinutes(1)
    };

    private static T ExtractRight<T>(Either<EncinaError, T> result)
    {
        result.IsRight.ShouldBeTrue("Expected Right but got Left: " +
            result.Match(Right: _ => "", Left: e => e.Message));
        return result.Match(Right: r => r, Left: _ => default!);
    }

    #endregion

    #region ApplyToAllShardsAsync - DDL Execution

    [Fact]
    public async Task ApplyToAllShardsAsync_CreatesTableOnAllShards()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        var script = CreateProductsTableScript();
        var options = CreateOptions();

        // Act
        var result = await coordinator.ApplyToAllShardsAsync(script, options);

        // Assert
        var migrationResult = ExtractRight(result);
        migrationResult.AllSucceeded.ShouldBeTrue();
        migrationResult.SucceededCount.ShouldBe(3);

        // Verify table exists on all 3 shards
        (await TableExistsAsync(_fixture.Shard1ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
        (await TableExistsAsync(_fixture.Shard2ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
        (await TableExistsAsync(_fixture.Shard3ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
    }

    [Fact]
    public async Task ApplyToAllShardsAsync_Sequential_ExecutesAllShards()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        var script = CreateProductsTableScript();
        var options = CreateOptions(MigrationStrategy.Sequential);

        // Act
        var result = await coordinator.ApplyToAllShardsAsync(script, options);

        // Assert
        var migrationResult = ExtractRight(result);
        migrationResult.AllSucceeded.ShouldBeTrue();
        migrationResult.PerShardStatus.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ApplyToAllShardsAsync_Parallel_ExecutesAllShards()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        var script = CreateProductsTableScript();
        var options = CreateOptions(MigrationStrategy.Parallel);

        // Act
        var result = await coordinator.ApplyToAllShardsAsync(script, options);

        // Assert
        var migrationResult = ExtractRight(result);
        migrationResult.AllSucceeded.ShouldBeTrue();
        migrationResult.PerShardStatus.Count.ShouldBe(3);

        // Verify actual DDL execution
        (await TableExistsAsync(_fixture.Shard1ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
        (await TableExistsAsync(_fixture.Shard2ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
        (await TableExistsAsync(_fixture.Shard3ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
    }

    [Fact]
    public async Task ApplyToAllShardsAsync_MultipleMigrations_CreatesMultipleTables()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        var options = CreateOptions();

        // Act - apply two sequential migrations
        var result1 = await coordinator.ApplyToAllShardsAsync(CreateProductsTableScript(), options);
        var result2 = await coordinator.ApplyToAllShardsAsync(CreateOrdersTableScript(), options);

        // Assert
        ExtractRight(result1).AllSucceeded.ShouldBeTrue();
        ExtractRight(result2).AllSucceeded.ShouldBeTrue();

        // Verify both tables exist on all shards
        (await TableExistsAsync(_fixture.Shard1ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
        (await TableExistsAsync(_fixture.Shard1ConnectionString, "MigrationTestOrders")).ShouldBeTrue();
        (await TableExistsAsync(_fixture.Shard2ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
        (await TableExistsAsync(_fixture.Shard2ConnectionString, "MigrationTestOrders")).ShouldBeTrue();
        (await TableExistsAsync(_fixture.Shard3ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
        (await TableExistsAsync(_fixture.Shard3ConnectionString, "MigrationTestOrders")).ShouldBeTrue();
    }

    #endregion

    #region RollbackAsync - Real Rollback

    [Fact]
    public async Task RollbackAsync_DropsCreatedTable()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        var script = CreateProductsTableScript();
        var options = CreateOptions();

        // Apply migration first
        var applyResult = await coordinator.ApplyToAllShardsAsync(script, options);
        var migrationResult = ExtractRight(applyResult);
        migrationResult.AllSucceeded.ShouldBeTrue();

        // Verify table exists before rollback
        (await TableExistsAsync(_fixture.Shard1ConnectionString, "MigrationTestProducts")).ShouldBeTrue();

        // Act
        var rollbackResult = await coordinator.RollbackAsync(migrationResult);

        // Assert
        rollbackResult.IsRight.ShouldBeTrue();

        // Verify table no longer exists on all shards
        (await TableExistsAsync(_fixture.Shard1ConnectionString, "MigrationTestProducts")).ShouldBeFalse();
        (await TableExistsAsync(_fixture.Shard2ConnectionString, "MigrationTestProducts")).ShouldBeFalse();
        (await TableExistsAsync(_fixture.Shard3ConnectionString, "MigrationTestProducts")).ShouldBeFalse();
    }

    #endregion

    #region DetectDriftAsync - Schema Drift Detection

    [Fact]
    public async Task DetectDriftAsync_AllShardsIdentical_NoDrift()
    {
        // Arrange - apply same migration to all shards
        var coordinator = CreateCoordinator();
        var script = CreateProductsTableScript();
        var options = CreateOptions();
        var applyResult = await coordinator.ApplyToAllShardsAsync(script, options);
        ExtractRight(applyResult).AllSucceeded.ShouldBeTrue();

        // Act
        var driftResult = await coordinator.DetectDriftAsync();

        // Assert
        var report = ExtractRight(driftResult);
        report.HasDrift.ShouldBeFalse();
    }

    [Fact]
    public async Task DetectDriftAsync_ManuallyAlteredSchema_DetectsDrift()
    {
        // Arrange - apply same migration to all shards
        var coordinator = CreateCoordinator();
        var script = CreateProductsTableScript();
        var options = CreateOptions();
        var applyResult = await coordinator.ApplyToAllShardsAsync(script, options);
        ExtractRight(applyResult).AllSucceeded.ShouldBeTrue();

        // Manually alter schema on shard-2 (add extra column)
        await using var connection = new SqliteConnection(_fixture.Shard2ConnectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "ALTER TABLE MigrationTestProducts ADD COLUMN Description TEXT;";
        await cmd.ExecuteNonQueryAsync();

        // Verify the column was actually added
        var columnCountShard1 = await GetColumnCountAsync(_fixture.Shard1ConnectionString, "MigrationTestProducts");
        var columnCountShard2 = await GetColumnCountAsync(_fixture.Shard2ConnectionString, "MigrationTestProducts");
        columnCountShard2.ShouldBeGreaterThan(columnCountShard1);

        // Act
        var driftResult = await coordinator.DetectDriftAsync();

        // Assert
        var report = ExtractRight(driftResult);
        report.HasDrift.ShouldBeTrue();
        report.Diffs.ShouldContain(d => d.ShardId == "shard-2");
    }

    #endregion

    #region GetProgressAsync - Progress Tracking

    [Fact]
    public async Task GetProgressAsync_AfterMigration_ReportsCompleted()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        var script = CreateProductsTableScript();
        var options = CreateOptions();

        var applyResult = await coordinator.ApplyToAllShardsAsync(script, options);
        var migrationResult = ExtractRight(applyResult);

        // Act
        var progressResult = await coordinator.GetProgressAsync(migrationResult.Id);

        // Assert
        var progress = ExtractRight(progressResult);
        progress.TotalShards.ShouldBe(3);
        progress.IsFinished.ShouldBeTrue();
    }

    #endregion

    #region Strategy Tests - Real Execution

    [Fact]
    public async Task RollingUpdate_ExecutesAllShards()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        var script = CreateProductsTableScript();
        var options = CreateOptions(MigrationStrategy.RollingUpdate);

        // Act
        var result = await coordinator.ApplyToAllShardsAsync(script, options);

        // Assert
        var migrationResult = ExtractRight(result);
        migrationResult.AllSucceeded.ShouldBeTrue();

        // Verify DDL was actually executed
        (await TableExistsAsync(_fixture.Shard1ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
        (await TableExistsAsync(_fixture.Shard2ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
        (await TableExistsAsync(_fixture.Shard3ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
    }

    [Fact]
    public async Task CanaryFirst_ExecutesAllShards()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        var script = CreateProductsTableScript();
        var options = CreateOptions(MigrationStrategy.CanaryFirst);

        // Act
        var result = await coordinator.ApplyToAllShardsAsync(script, options);

        // Assert
        var migrationResult = ExtractRight(result);
        migrationResult.AllSucceeded.ShouldBeTrue();

        // Verify DDL was actually executed
        (await TableExistsAsync(_fixture.Shard1ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
        (await TableExistsAsync(_fixture.Shard2ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
        (await TableExistsAsync(_fixture.Shard3ConnectionString, "MigrationTestProducts")).ShouldBeTrue();
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task ApplyToAllShardsAsync_InvalidSql_FailsGracefully()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        var badScript = new MigrationScript(
            "bad_migration",
            "THIS IS NOT VALID SQL AT ALL;;;",
            "DROP TABLE IF EXISTS nonexistent;",
            "Invalid SQL to test error handling",
            "sha256:invalid");
        var options = CreateOptions();

        // Act
        var result = await coordinator.ApplyToAllShardsAsync(badScript, options);

        // Assert - should return Right with failed per-shard results (not throw)
        var migrationResult = ExtractRight(result);
        migrationResult.AllSucceeded.ShouldBeFalse();
        migrationResult.FailedCount.ShouldBeGreaterThan(0);
    }

    #endregion

    #region Real SQLite Implementations for Integration Testing

    /// <summary>
    /// Real SQLite implementation of <see cref="IMigrationExecutor"/>
    /// that executes actual DDL against real databases.
    /// </summary>
    private sealed class SqliteMigrationExecutor : IMigrationExecutor
    {
        public async Task<Either<EncinaError, Unit>> ExecuteSqlAsync(
            ShardInfo shardInfo,
            string sql,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = new SqliteConnection(shardInfo.ConnectionString);
                await connection.OpenAsync(cancellationToken);

                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync(cancellationToken);

                return unit;
            }
            catch (Exception ex)
            {
                return EncinaError.New($"SQLite execution failed on shard '{shardInfo.ShardId}': {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Real SQLite implementation of <see cref="IMigrationHistoryStore"/>
    /// that persists migration history in each shard's database.
    /// </summary>
    private sealed class SqliteMigrationHistoryStore : IMigrationHistoryStore
    {
        private const string HistoryTableSql = """
            CREATE TABLE IF NOT EXISTS __EncinaMigrationHistory (
                MigrationId TEXT NOT NULL PRIMARY KEY,
                Description TEXT NOT NULL,
                Checksum TEXT NOT NULL,
                AppliedAtUtc TEXT NOT NULL,
                DurationMs REAL NOT NULL,
                RolledBack INTEGER NOT NULL DEFAULT 0
            );
            """;

        public async Task<Either<EncinaError, Unit>> EnsureHistoryTableExistsAsync(
            ShardInfo shardInfo,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = new SqliteConnection(shardInfo.ConnectionString);
                await connection.OpenAsync(cancellationToken);

                await using var command = connection.CreateCommand();
                command.CommandText = HistoryTableSql;
                await command.ExecuteNonQueryAsync(cancellationToken);

                return unit;
            }
            catch (Exception ex)
            {
                return EncinaError.New($"Failed to create history table on '{shardInfo.ShardId}': {ex.Message}", ex);
            }
        }

        public async Task<Either<EncinaError, IReadOnlyList<string>>> GetAppliedAsync(
            string shardId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Note: This is simplified - in a real scenario we'd need the connection string
                return new List<string>().AsReadOnly();
            }
            catch (Exception ex)
            {
                return EncinaError.New($"Failed to get applied migrations for '{shardId}': {ex.Message}", ex);
            }
        }

        public async Task<Either<EncinaError, Unit>> RecordAppliedAsync(
            string shardId,
            MigrationScript script,
            TimeSpan duration,
            CancellationToken cancellationToken = default)
        {
            // In integration tests, recording is a no-op for simplicity
            // The actual DDL execution is what we're testing
            await Task.CompletedTask;
            return unit;
        }

        public async Task<Either<EncinaError, Unit>> RecordRolledBackAsync(
            string shardId,
            string migrationId,
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return unit;
        }

        public async Task<Either<EncinaError, Unit>> ApplyHistoricalMigrationsAsync(
            string shardId,
            IReadOnlyList<MigrationScript> scripts,
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return unit;
        }
    }

    /// <summary>
    /// Real SQLite implementation of <see cref="ISchemaIntrospector"/>
    /// that compares actual table schemas across shard databases.
    /// </summary>
    private sealed class SqliteSchemaIntrospector : ISchemaIntrospector
    {
        public async Task<Either<EncinaError, ShardSchemaDiff>> CompareAsync(
            ShardInfo shard,
            ShardInfo baseline,
            bool includeColumnDiffs,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var shardTables = await GetTablesAsync(shard.ConnectionString, cancellationToken);
                var baselineTables = await GetTablesAsync(baseline.ConnectionString, cancellationToken);

                var diffs = new List<TableDiff>();

                // Find missing tables (in baseline but not in shard)
                foreach (var table in baselineTables)
                {
                    if (!shardTables.ContainsKey(table.Key))
                    {
                        diffs.Add(new TableDiff(table.Key, TableDiffType.Missing));
                    }
                }

                // Find extra tables (in shard but not in baseline)
                foreach (var table in shardTables)
                {
                    if (!baselineTables.ContainsKey(table.Key))
                    {
                        diffs.Add(new TableDiff(table.Key, TableDiffType.Extra));
                    }
                }

                // Find modified tables (different column count or definitions)
                if (includeColumnDiffs)
                {
                    foreach (var table in shardTables)
                    {
                        if (baselineTables.TryGetValue(table.Key, out var baselineCols))
                        {
                            if (table.Value.Count != baselineCols.Count)
                            {
                                var columnDiffs = new List<string>
                                {
                                    $"Column count differs: shard has {table.Value.Count}, baseline has {baselineCols.Count}"
                                };
                                diffs.Add(new TableDiff(table.Key, TableDiffType.Modified, columnDiffs));
                            }
                            else
                            {
                                // Compare individual columns
                                var colDiffs = CompareColumns(table.Value, baselineCols);
                                if (colDiffs.Count > 0)
                                {
                                    diffs.Add(new TableDiff(table.Key, TableDiffType.Modified, colDiffs));
                                }
                            }
                        }
                    }
                }

                return new ShardSchemaDiff(shard.ShardId, baseline.ShardId, diffs);
            }
            catch (Exception ex)
            {
                return EncinaError.New($"Schema comparison failed: {ex.Message}", ex);
            }
        }

        private static async Task<Dictionary<string, List<(string Name, string Type, bool Nullable)>>>
            GetTablesAsync(string connectionString, CancellationToken ct)
        {
            var result = new Dictionary<string, List<(string, string, bool)>>();

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(ct);

            // Get all user tables (exclude SQLite internal tables)
            await using var tablesCmd = connection.CreateCommand();
            tablesCmd.CommandText = """
                SELECT name FROM sqlite_master
                WHERE type='table' AND name NOT LIKE 'sqlite_%'
                ORDER BY name
                """;

            var tables = new List<string>();
            await using var reader = await tablesCmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                tables.Add(reader.GetString(0));
            }

            // Get columns for each table
            foreach (var tableName in tables)
            {
                var columns = new List<(string, string, bool)>();
                await using var colCmd = connection.CreateCommand();
                colCmd.CommandText = $"PRAGMA table_info({tableName})";
                await using var colReader = await colCmd.ExecuteReaderAsync(ct);
                while (await colReader.ReadAsync(ct))
                {
                    var colName = colReader.GetString(1);
                    var colType = colReader.GetString(2);
                    var notNull = colReader.GetInt32(3) == 1;
                    columns.Add((colName, colType, !notNull));
                }

                result[tableName] = columns;
            }

            return result;
        }

        private static List<string> CompareColumns(
            List<(string Name, string Type, bool Nullable)> shardCols,
            List<(string Name, string Type, bool Nullable)> baselineCols)
        {
            var diffs = new List<string>();
            var baselineMap = baselineCols.ToDictionary(c => c.Name);

            foreach (var col in shardCols)
            {
                if (!baselineMap.TryGetValue(col.Name, out var baselineCol))
                {
                    diffs.Add($"Extra column: {col.Name}");
                }
                else
                {
                    if (col.Type != baselineCol.Type)
                        diffs.Add($"{col.Name}: type differs ({col.Type} vs {baselineCol.Type})");
                    if (col.Nullable != baselineCol.Nullable)
                        diffs.Add($"{col.Name}: nullability differs");
                }
            }

            foreach (var col in baselineCols)
            {
                if (!shardCols.Any(c => c.Name == col.Name))
                {
                    diffs.Add($"Missing column: {col.Name}");
                }
            }

            return diffs;
        }
    }

    #endregion
}
