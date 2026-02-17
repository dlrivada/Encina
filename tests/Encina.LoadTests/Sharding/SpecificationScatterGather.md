# Load Tests - Specification-Based Scatter-Gather for Sharding

## Status: Not Implemented

## Justification

Load tests for specification-based scatter-gather are not implemented for the following reasons:

### 1. Scatter-Gather Performance is Already Covered

The existing `ShardedQueryExecutor` load tests validate the core scatter-gather infrastructure:
- Parallel query dispatch across shards
- Connection pool management under concurrent load
- Timeout and cancellation behavior under pressure
- Partial failure handling at scale

Specification-based scatter-gather reuses this same infrastructure; it only adds specification-to-query translation before dispatching.

### 2. Specification Translation Adds Minimal Overhead

The specification evaluation is a one-time, per-shard operation:
- EF Core: `specification.ToExpression()` is applied to `IQueryable<T>` (expression tree composition)
- Dapper/ADO.NET: `SpecificationSqlBuilder` generates SQL once per shard query
- MongoDB: `SpecificationFilterBuilder` generates `FilterDefinition<T>` once per shard

This overhead is negligible compared to network I/O and database query execution.

### 3. Merge Operation is CPU-Bound and Fast

`ScatterGatherResultMerger.MergeAndOrder<T>()` operates on in-memory collections:
- Collects results from all shards (already in memory after scatter-gather)
- Applies LINQ `OrderBy` on the merged list
- Pagination via `Skip`/`Take` on the ordered list

For typical page sizes (10-100 items per shard × 3-10 shards), this completes in microseconds.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify specification application per shard, result merging, pagination strategies
- **Guard Tests**: Ensure parameter validation for all public methods
- **Property Tests**: Verify pagination invariants and merge completeness across random inputs
- **Contract Tests**: Confirm all 13 providers implement `IShardedSpecificationSupport`
- **Integration Tests**: Validate real database scatter-gather with specifications
- **Benchmarks**: Micro-benchmarks for specification evaluation and merge overhead

### 5. Recommended Alternative

If scatter-gather load testing is needed in the future, extend the existing `ShardedQueryExecutor` load tests to include specification-based queries alongside lambda-based queries. This would validate that specification overhead does not degrade performance under concurrent load.

## Related Files

- `src/Encina/Sharding/Execution/ShardedQueryExecutor.cs` - Core scatter-gather executor
- `src/Encina.DomainModeling/Sharding/ScatterGatherResultMerger.cs` - Result merge logic
- `src/Encina.DomainModeling/Sharding/IShardedSpecificationSupport.cs` - Interface definition
- `tests/Encina.UnitTests/Core/Sharding/Specification/` - Unit tests
- `tests/Encina.PropertyTests/Database/Sharding/ShardedSpecificationPropertyTests.cs` - Property tests

## Date: 2026-02-12
## Issue: #652
