using Encina.Sharding.Migrations;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Migrations;

/// <summary>
/// Property-based tests for <see cref="SchemaDriftReport"/> invariants and
/// <see cref="SchemaComparer"/> correctness.
/// Verifies that the <see cref="SchemaDriftReport.HasDrift"/> computed property is
/// consistent with the contents of the <see cref="SchemaDriftReport.Diffs"/> collection,
/// and that the schema comparer produces correct diff results.
/// </summary>
[Trait("Category", "Property")]
public sealed class SchemaDriftReportProperties
{
    #region HasDrift with Empty Diffs

    /// <summary>
    /// HasDrift is false when the Diffs list is empty.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool HasDrift_IsFalse_WhenDiffsListIsEmpty()
    {
        var report = new SchemaDriftReport(
            Array.Empty<ShardSchemaDiff>(), DateTimeOffset.UtcNow);

        return !report.HasDrift;
    }

    #endregion

    #region HasDrift with Empty TableDiffs

    /// <summary>
    /// HasDrift is false when all ShardSchemaDiff entries have empty TableDiffs.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property HasDrift_IsFalse_WhenAllDiffsHaveEmptyTableDiffs()
    {
        var gen = Gen.Choose(1, 5).Select(count =>
        {
            var diffs = Enumerable.Range(0, count)
                .Select(i => new ShardSchemaDiff(
                    $"shard-{i}", "shard-baseline", Array.Empty<TableDiff>()))
                .ToList();
            return new SchemaDriftReport(diffs, DateTimeOffset.UtcNow);
        });

        return Prop.ForAll(Arb.From(gen), report =>
            !report.HasDrift);
    }

    #endregion

    #region HasDrift with Non-Empty TableDiffs

    /// <summary>
    /// HasDrift is true when at least one ShardSchemaDiff has non-empty TableDiffs.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property HasDrift_IsTrue_WhenAnyDiffHasNonEmptyTableDiffs()
    {
        var tableDiffGen = Gen.Elements(
            new TableDiff("orders", TableDiffType.Missing),
            new TableDiff("users", TableDiffType.Extra),
            new TableDiff("products", TableDiffType.Modified, ["Missing column: price"]),
            new TableDiff("events", TableDiffType.Missing),
            new TableDiff("sessions", TableDiffType.Extra));

        var gen = Gen.Choose(1, 3).SelectMany(tableDiffCount =>
            Gen.ArrayOf(tableDiffGen, tableDiffCount).SelectMany(tableDiffs =>
                Gen.Choose(0, 3).Select(extraCount =>
                {
                    var driftedDiff = new ShardSchemaDiff(
                        "shard-drifted", "shard-baseline", tableDiffs);

                    var extraDiffs = Enumerable.Range(0, extraCount)
                        .Select(i => new ShardSchemaDiff(
                            $"shard-extra-{i}", "shard-baseline", Array.Empty<TableDiff>()))
                        .ToList();

                    var allDiffs = new List<ShardSchemaDiff> { driftedDiff };
                    allDiffs.AddRange(extraDiffs);

                    return new SchemaDriftReport(allDiffs, DateTimeOffset.UtcNow);
                })));

        return Prop.ForAll(Arb.From(gen), report =>
            report.HasDrift);
    }

    #endregion

    #region HasDrift Consistency

    /// <summary>
    /// HasDrift always matches the manual computation:
    /// Diffs.Count > 0 AND any diff has non-empty TableDiffs.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property HasDrift_MatchesManualComputation()
    {
        var gen = Gen.Choose(0, 5).SelectMany(count =>
            Gen.ArrayOf(Gen.Elements(true, false), Math.Max(count, 1)).Select(hasTableDiffsFlags =>
            {
                var diffs = Enumerable.Range(0, count)
                    .Select(i =>
                    {
                        var hasDiffs = i < hasTableDiffsFlags.Length && hasTableDiffsFlags[i];
                        IReadOnlyList<TableDiff> tds = hasDiffs
                            ? [new TableDiff($"table-{i}", TableDiffType.Missing)]
                            : Array.Empty<TableDiff>();
                        return new ShardSchemaDiff($"shard-{i}", "shard-baseline", tds);
                    })
                    .ToList();
                return new SchemaDriftReport(diffs, DateTimeOffset.UtcNow);
            }));

        return Prop.ForAll(Arb.From(gen), report =>
        {
            var expectedHasDrift = report.Diffs.Count > 0 &&
                                   report.Diffs.Any(d => d.TableDiffs.Count > 0);
            return report.HasDrift == expectedHasDrift;
        });
    }

    #endregion

    #region SchemaComparer Invariants

    /// <summary>
    /// Comparing a schema against itself (identical table set) produces zero diffs.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SchemaComparer_IdenticalSchemas_ProducesEmptyDiffs()
    {
        return Prop.ForAll(Arb.From(BuildSchemaGen()), schema =>
        {
            var baseline = new ShardSchema(
                "shard-baseline", schema.Tables, DateTimeOffset.UtcNow);
            var diff = SchemaComparer.Compare(
                "shard-0", "shard-baseline", schema, baseline, includeColumnDiffs: true);

            return diff.TableDiffs.Count == 0;
        });
    }

    /// <summary>
    /// When the shard is missing tables present in the baseline, Missing diffs are produced.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SchemaComparer_MissingTables_DetectedAsMissing()
    {
        return Prop.ForAll(Arb.From(BuildSchemaWithAtLeastTwoTablesGen()), tables =>
        {
            var baselineSchema = new ShardSchema("shard-baseline", tables, DateTimeOffset.UtcNow);
            var shardTables = tables.Take(tables.Count - 1).ToList();
            var shardSchema = new ShardSchema("shard-0", shardTables, DateTimeOffset.UtcNow);

            var diff = SchemaComparer.Compare(
                "shard-0", "shard-baseline", shardSchema, baselineSchema, includeColumnDiffs: false);

            return diff.TableDiffs.Any(d => d.DiffType == TableDiffType.Missing);
        });
    }

    /// <summary>
    /// When the shard has extra tables not in the baseline, Extra diffs are produced.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SchemaComparer_ExtraTables_DetectedAsExtra()
    {
        return Prop.ForAll(Arb.From(BuildSchemaGen()), schema =>
        {
            var extraTable = new TableSchema(
                "extra_unique_table", [new ColumnSchema("id", "integer", false)]);
            var shardTables = schema.Tables.Append(extraTable).ToList();

            var baselineSchema = new ShardSchema(
                "shard-baseline", schema.Tables, DateTimeOffset.UtcNow);
            var shardSchema = new ShardSchema(
                "shard-0", shardTables, DateTimeOffset.UtcNow);

            var diff = SchemaComparer.Compare(
                "shard-0", "shard-baseline", shardSchema, baselineSchema, includeColumnDiffs: false);

            return diff.TableDiffs.Any(d => d.DiffType == TableDiffType.Extra);
        });
    }

    /// <summary>
    /// All diff type counts (Missing, Extra, Modified) are non-negative.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SchemaComparer_DiffCounts_AreNonNegative()
    {
        var gen = BuildSchemaGen().SelectMany(schema1 =>
            BuildSchemaGen().Select(schema2 => (Schema1: schema1, Schema2: schema2)));

        return Prop.ForAll(Arb.From(gen), pair =>
        {
            var shard = new ShardSchema(
                "shard-0", pair.Schema1.Tables, DateTimeOffset.UtcNow);
            var baseline = new ShardSchema(
                "shard-baseline", pair.Schema2.Tables, DateTimeOffset.UtcNow);

            var diff = SchemaComparer.Compare(
                "shard-0", "shard-baseline", shard, baseline, includeColumnDiffs: true);

            var missing = diff.TableDiffs.Count(d => d.DiffType == TableDiffType.Missing);
            var extra = diff.TableDiffs.Count(d => d.DiffType == TableDiffType.Extra);
            var modified = diff.TableDiffs.Count(d => d.DiffType == TableDiffType.Modified);

            return missing >= 0 && extra >= 0 && modified >= 0;
        });
    }

    #endregion

    #region Generators

    private static Gen<ShardSchema> BuildSchemaGen()
    {
        var columnGen = Gen.Elements(
            new ColumnSchema("id", "integer", false),
            new ColumnSchema("name", "text", true),
            new ColumnSchema("status", "varchar(50)", false),
            new ColumnSchema("created_at", "timestamp", false));

        var tableNameGen = Gen.Elements("orders", "users", "events", "products");

        var tableGen = tableNameGen.SelectMany(name =>
            Gen.Choose(1, 4).SelectMany(colCount =>
                Gen.ArrayOf(columnGen, colCount).Select(cols =>
                    new TableSchema(name, cols.DistinctBy(c => c.Name).ToList()))));

        return Gen.Choose(1, 3).SelectMany(tableCount =>
            Gen.ArrayOf(tableGen, tableCount).Select(tables =>
            {
                var distinctTables = tables.DistinctBy(t => t.Name).ToList();
                return new ShardSchema("shard-0", distinctTables, DateTimeOffset.UtcNow);
            }));
    }

    private static Gen<IReadOnlyList<TableSchema>> BuildSchemaWithAtLeastTwoTablesGen()
    {
        // Pre-defined table name sets, each with at least 2 tables
        var tableNameSets = new[]
        {
            new[] { "orders", "users" },
            new[] { "events", "products" },
            new[] { "orders", "users", "events" },
            new[] { "products", "sessions", "orders" },
        };

        return Gen.Elements(tableNameSets).Select(nameSet =>
            (IReadOnlyList<TableSchema>)nameSet
                .Select(name => new TableSchema(
                    name, [new ColumnSchema("id", "integer", false)]))
                .ToList());
    }

    #endregion
}
