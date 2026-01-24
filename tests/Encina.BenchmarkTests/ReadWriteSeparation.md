# Benchmark Tests - Read/Write Separation Pattern

## Status: Not Implemented

## Justification

Benchmark tests for the Read/Write Separation pattern are not implemented for the following reasons:

### 1. Routing Operations Are Trivial

All Read/Write Separation operations are O(1):

- `GetWriteConnectionString()` - Direct property access
- `GetReadConnectionString()` - Strategy selection + array index
- `GetConnectionString()` - Routing context check + above operations
- `DatabaseRoutingScope` - `AsyncLocal<T>.Value` assignment

### 2. Performance Is Dominated by Database Operations

The connection factory creates database connections. The actual performance characteristics are determined by:

- Database driver implementation (SqlClient, Npgsql, MySqlConnector, etc.)
- Connection pooling behavior
- Network latency to database servers
- Database server response time

Benchmarking the wrapper code around these operations would be noise.

### 3. Replica Selection Strategies Are Sub-Microsecond

```csharp
// The actual code being "benchmarked"
public string GetReadConnectionString()
{
    var replicas = _options.ReadConnectionStrings;
    if (replicas.Count == 0)
        return _options.WriteConnectionString ?? string.Empty;

    // Strategy selection: ~10ns
    var index = _strategy.SelectReplica(replicas.Count);
    return replicas[index];
}
```

Each strategy:
- **RoundRobin**: `Interlocked.Increment()` - ~5ns
- **Random**: `Random.Next()` - ~20ns
- **LeastConnections**: Loop + comparison - ~50ns

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify behavior correctness
- **Contract Tests**: Verify API consistency across all 12 providers
- **Integration Tests**: (When implemented) Test actual routing to replicas

### 5. Recommended Benchmarks (if needed)

```csharp
[MemoryDiagnoser]
public class ReadWriteSeparationBenchmarks
{
    private readonly IReadWriteConnectionSelector _selector;
    private readonly ReadWriteConnectionFactory _factory;

    [GlobalSetup]
    public void Setup()
    {
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Data Source=:memory:",
            ReadConnectionStrings = { "Data Source=:memory:", "Data Source=:memory:" }
        };
        _selector = new ReadWriteConnectionSelector(options);
        _factory = new ReadWriteConnectionFactory(_selector);
    }

    [Benchmark(Baseline = true)]
    public string GetWriteConnectionString()
    {
        return _selector.GetWriteConnectionString();
    }

    [Benchmark]
    public string GetReadConnectionString()
    {
        return _selector.GetReadConnectionString();
    }

    [Benchmark]
    public IDbConnection CreateReadConnection()
    {
        var conn = _factory.CreateReadConnection();
        conn.Dispose();
        return conn;
    }
}
```

### 6. Why Not Include These Benchmarks Now

- **Connection string retrieval**: Property access + array index, ~10ns
- **Connection creation**: Dominated by driver constructor, ~500ns
- **Routing scope**: `AsyncLocal<T>` assignment, ~20ns
- **None of these are hot paths**: Application code dominates

## Related Files

- `src/Encina.Messaging/ReadWriteSeparation/` - Core abstractions
- `src/Encina.*/ReadWriteSeparation/` - Provider implementations (12 providers)
- `tests/Encina.UnitTests/*/ReadWriteSeparation/` - Unit tests
- `tests/Encina.ContractTests/Database/ReadWriteSeparation/` - Contract tests

## Date: 2026-01-24

## Issue: #283
