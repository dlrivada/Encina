# Distributed Lock Performance

Performance characteristics of Encina's distributed locking primitives. Data auto-generated from BenchmarkDotNet measurements.

## In-Memory Lock Provider

<!-- docref-table: bench:lock/* -->
<!-- /docref-table -->

## Notes

- These benchmarks measure the **in-memory** provider. Redis and SQL Server lock providers have higher latency due to network round-trips — see [load test baselines](../testing/load-test-baselines.md) for those.
- Contention behavior under high parallelism is measured separately in the [load tests dashboard](https://dlrivada.github.io/Encina/load-tests/dashboard/).

*Auto-generated from benchmark data. See [methodology](../testing/performance-measurement-methodology.md).*
