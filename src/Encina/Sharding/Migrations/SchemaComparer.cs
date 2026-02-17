namespace Encina.Sharding.Migrations;

/// <summary>
/// Compares two <see cref="ShardSchema"/> instances and produces a <see cref="ShardSchemaDiff"/>.
/// </summary>
/// <remarks>
/// Shared comparison logic used by all provider-specific schema introspectors.
/// </remarks>
internal static class SchemaComparer
{
    /// <summary>
    /// Compares the schema of a shard against a baseline schema.
    /// </summary>
    internal static ShardSchemaDiff Compare(
        string shardId,
        string baselineShardId,
        ShardSchema shardSchema,
        ShardSchema baselineSchema,
        bool includeColumnDiffs)
    {
        var baselineTables = baselineSchema.Tables.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        var shardTables = shardSchema.Tables.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        var diffs = new List<TableDiff>();

        // Tables in baseline but missing from shard
        foreach (var baselineTable in baselineTables)
        {
            if (!shardTables.ContainsKey(baselineTable.Key))
            {
                diffs.Add(new TableDiff(baselineTable.Key, TableDiffType.Missing));
            }
        }

        // Tables in shard but not in baseline (extra)
        foreach (var shardTable in shardTables)
        {
            if (!baselineTables.ContainsKey(shardTable.Key))
            {
                diffs.Add(new TableDiff(shardTable.Key, TableDiffType.Extra));
            }
        }

        // Tables in both — check for column-level differences
        if (includeColumnDiffs)
        {
            foreach (var shardTable in shardTables)
            {
                if (baselineTables.TryGetValue(shardTable.Key, out var baselineTable))
                {
                    var columnDiffs = CompareColumns(shardTable.Value, baselineTable);
                    if (columnDiffs.Count > 0)
                    {
                        diffs.Add(new TableDiff(shardTable.Key, TableDiffType.Modified, columnDiffs));
                    }
                }
            }
        }

        return new ShardSchemaDiff(shardId, baselineShardId, diffs);
    }

    private static List<string> CompareColumns(TableSchema shardTable, TableSchema baselineTable)
    {
        var baselineColumns = baselineTable.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        var shardColumns = shardTable.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        var diffs = new List<string>();

        foreach (var bc in baselineColumns)
        {
            if (!shardColumns.ContainsKey(bc.Key))
            {
                diffs.Add($"Missing column: {bc.Key}");
            }
        }

        foreach (var sc in shardColumns)
        {
            if (!baselineColumns.ContainsKey(sc.Key))
            {
                diffs.Add($"Extra column: {sc.Key}");
            }
        }

        foreach (var sc in shardColumns)
        {
            if (baselineColumns.TryGetValue(sc.Key, out var bc))
            {
                if (!string.Equals(sc.Value.DataType, bc.DataType, StringComparison.OrdinalIgnoreCase))
                {
                    diffs.Add($"Column '{sc.Key}' type differs: '{sc.Value.DataType}' vs '{bc.DataType}'");
                }

                if (sc.Value.IsNullable != bc.IsNullable)
                {
                    diffs.Add($"Column '{sc.Key}' nullability differs: {sc.Value.IsNullable} vs {bc.IsNullable}");
                }
            }
        }

        return diffs;
    }
}
