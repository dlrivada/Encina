# ID Generation Strategy Comparison

Performance comparison of Encina's ID generation strategies. Data auto-generated from BenchmarkDotNet measurements.

See [ADR-011](../architecture/adr/011-id-generation-multi-strategy.md) for the architectural decision behind multi-strategy support.

## Generation Throughput

<!-- docref-table: bench:idgen/* -->
<!-- /docref-table -->

## How to Read This Data

- **Mean/Median**: typical time per ID generation call
- **Allocated**: memory allocated per call (lower is better)
- **Stable**: whether the measurement is deterministic enough for citation (CoV ≤ 10%)

*Auto-generated from benchmark data. See [methodology](../testing/performance-measurement-methodology.md) for formulas and variance handling.*
