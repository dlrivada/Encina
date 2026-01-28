# Benchmark Documentation

This directory contains benchmark result templates and analysis guides for Encina performance testing.

## Documents

| Document | Description |
|----------|-------------|
| [dapper-vs-ado-comparison.md](dapper-vs-ado-comparison.md) | Direct comparison between Dapper and ADO.NET providers |
| [provider-sql-dialect-comparison.md](provider-sql-dialect-comparison.md) | SQL generation performance across database providers |

## Purpose

These templates serve to:

1. **Standardize Results**: Consistent format for benchmark reporting
2. **Track History**: Document performance changes over time
3. **Guide Analysis**: Help interpret benchmark results
4. **Enable Comparison**: Compare results across environments and versions

## Updating Benchmark Results

### 1. Run Benchmarks

```bash
# Dapper vs ADO comparison
cd tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks
dotnet run -c Release -- --filter "*DapperVsAdo*" --exporters markdown

# SQL dialect comparison
dotnet run -c Release -- --filter "*SpecificationSqlBuilder*" --exporters markdown
```

### 2. Locate Results

Results are exported to:
```
artifacts/performance/results/
```

Or the default BenchmarkDotNet location:
```
BenchmarkDotNet.Artifacts/results/
```

### 3. Update Templates

1. Open the relevant markdown file in this directory
2. Copy benchmark results into the template tables
3. Update the "Benchmark Environment" section
4. Add entry to "Change History"

## Benchmark Projects

| Project | Location | Focus |
|---------|----------|-------|
| Encina.Dapper.Benchmarks | `tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks/` | Dapper provider stores, repository, SQL builder |
| Encina.ADO.Benchmarks | `tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks/` | ADO.NET provider stores, repository, SQL builder |
| Encina.Benchmarks | `tests/Encina.BenchmarkTests/Encina.Benchmarks/` | EF Core, general benchmarks |

## When to Run Benchmarks

- **Before Release**: Verify performance hasn't regressed
- **After Optimization**: Measure improvement impact
- **Provider Updates**: Check for provider-specific changes
- **Infrastructure Changes**: Validate new environment performance

## Interpreting Results

### Key Metrics

| Metric | Description | Guidance |
|--------|-------------|----------|
| **Mean** | Average execution time | Primary comparison metric |
| **Error** | Half of 99.9% confidence interval | Lower is more reliable |
| **StdDev** | Standard deviation | Lower is more consistent |
| **Ratio** | Comparison to baseline | 1.00 = same as baseline |
| **Allocated** | Heap memory allocated | Lower reduces GC pressure |

### Ratio Interpretation

| Ratio | Interpretation |
|-------|----------------|
| < 0.90 | Significantly faster (>10% improvement) |
| 0.90 - 1.10 | Similar performance (within 10%) |
| 1.10 - 1.25 | Moderately slower (10-25% overhead) |
| > 1.25 | Significantly slower (investigate) |

### Memory Allocation

| Allocation | Guidance |
|------------|----------|
| 0 B | Ideal for hot paths |
| < 1 KB | Acceptable for most operations |
| 1-10 KB | Consider optimization if called frequently |
| > 10 KB | Investigate allocation sources |

## Related Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Main Benchmark README](../../tests/Encina.BenchmarkTests/README.md)
- [Benchmark Results](../testing/benchmarks/benchmark-results.md)
