# Dapper vs ADO.NET Performance Comparison

This document provides benchmark result templates and analysis for comparing Dapper and ADO.NET provider implementations.

## Overview

The `DapperVsAdoComparisonBenchmarks` class directly compares Dapper micro-ORM overhead against pure ADO.NET implementations for identical operations.

## Benchmark Results Template

### Batch Read Operations

#### OutboxStore.GetPendingMessagesAsync

| Provider | BatchSize | Mean | Error | StdDev | Ratio | Allocated |
|----------|-----------|------|-------|--------|-------|-----------|
| Dapper | 10 | - μs | - μs | - μs | 1.00 | - KB |
| ADO | 10 | - μs | - μs | - μs | - | - KB |
| Dapper | 100 | - μs | - μs | - μs | 1.00 | - KB |
| ADO | 100 | - μs | - μs | - μs | - | - KB |

#### ScheduledMessageStore.GetDueMessagesAsync

| Provider | BatchSize | Mean | Error | StdDev | Ratio | Allocated |
|----------|-----------|------|-------|--------|-------|-----------|
| Dapper | 10 | - μs | - μs | - μs | 1.00 | - KB |
| ADO | 10 | - μs | - μs | - μs | - | - KB |
| Dapper | 100 | - μs | - μs | - μs | 1.00 | - KB |
| ADO | 100 | - μs | - μs | - μs | - | - KB |

### Single Write Operations

#### InboxStore.AddAsync

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| Dapper | - μs | - μs | - μs | 1.00 | - B |
| ADO | - μs | - μs | - μs | - | - B |

#### OutboxStore.AddAsync

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| Dapper | - μs | - μs | - μs | 1.00 | - B |
| ADO | - μs | - μs | - μs | - | - B |

### Parameterized Queries

#### InboxStore.GetMessageAsync

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| Dapper | - μs | - μs | - μs | 1.00 | - B |
| ADO | - μs | - μs | - μs | - | - B |

### Status Updates

#### OutboxStore.MarkAsProcessedAsync

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| Dapper | - μs | - μs | - μs | 1.00 | - B |
| ADO | - μs | - μs | - μs | - | - B |

#### OutboxStore.MarkAsFailedAsync

| Provider | Mean | Error | StdDev | Ratio | Allocated |
|----------|------|-------|--------|-------|-----------|
| Dapper | - μs | - μs | - μs | 1.00 | - B |
| ADO | - μs | - μs | - μs | - | - B |

### Raw Query Comparison

#### Direct Query (No Store Abstraction)

| Provider | BatchSize | Mean | Error | StdDev | Ratio | Allocated |
|----------|-----------|------|-------|--------|-------|-----------|
| Dapper | 10 | - μs | - μs | - μs | 1.00 | - KB |
| ADO | 10 | - μs | - μs | - μs | - | - KB |
| Dapper | 100 | - μs | - μs | - μs | 1.00 | - KB |
| ADO | 100 | - μs | - μs | - μs | - | - KB |

## Expected Results

Based on architectural analysis:

### Dapper Overhead Sources

1. **Object Mapping**: ~5-15% for single entity reads
2. **Parameter Handling**: ~2-5% for parameterized queries
3. **Type Conversion**: Variable depending on data types
4. **Caching**: First call has initialization cost, subsequent calls cached

### ADO.NET Advantages

1. **Direct Control**: No abstraction layer
2. **Minimal Allocations**: Manual mapping can avoid intermediate objects
3. **Provider-Specific Optimizations**: Can use native features directly

### Expected Overhead Ranges

| Operation Type | Expected Dapper Overhead |
|----------------|-------------------------|
| Single read | 5-15% |
| Batch read (small) | 10-20% |
| Batch read (large) | 15-25% |
| Single write | 5-10% |
| Parameterized query | 5-15% |

## Interpreting Results

### Ratio Column

- **Ratio < 1.10**: Dapper overhead is negligible (<10%)
- **Ratio 1.10-1.25**: Dapper overhead is acceptable (10-25%)
- **Ratio > 1.25**: Consider ADO.NET for hot paths

### Allocation Column

- Compare allocated bytes between providers
- Dapper typically allocates more due to:
  - DynamicParameters object
  - Compiled delegates
  - Type handler infrastructure

### When to Choose Dapper

- Developer productivity is priority
- Moderate query complexity
- Maintenance is important
- Type safety benefits outweigh overhead

### When to Choose ADO.NET

- Extreme performance requirements
- Very high throughput scenarios
- Simple queries with known structure
- Memory-constrained environments

## Running Benchmarks

```bash
# Run full comparison
cd tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks
dotnet run -c Release -- --filter "*DapperVsAdoComparisonBenchmarks*"

# Export results
dotnet run -c Release -- --filter "*DapperVsAdo*" --exporters json markdown

# Quick validation
dotnet run -c Release -- --filter "*DapperVsAdo*" --job short
```

## Updating This Document

1. Run benchmarks with `--exporters markdown`
2. Copy results from `BenchmarkDotNet.Artifacts/results/`
3. Update tables in this document
4. Add date and environment info below

## Benchmark Environment

- **Date**: [To be filled]
- **Machine**: [To be filled]
- **OS**: [To be filled]
- **.NET Version**: .NET 10
- **BenchmarkDotNet Version**: [To be filled]

## Change History

| Date | Change | Notes |
|------|--------|-------|
| 2026-01-28 | Initial template created | Issue #568 |
