# Caching Provider Performance

Performance comparison of Encina's caching providers and operations. Data auto-generated from BenchmarkDotNet measurements.

See [ADR-003](../architecture/adr/003-caching-strategy.md) for the architectural decision behind the caching strategy.

## Provider Operations (Memory vs Hybrid vs Redis)

See per-provider results docs for detailed tables:

- [Encina.Caching.Memory](caching-memory-benchmark-results.md)
- [Encina.Caching.Hybrid](caching-hybrid-benchmark-results.md)
- [Encina.Caching.Redis](caching-redis-benchmark-results.md)

<!-- docref-table: bench:caching-memory/* -->
<!-- /docref-table -->

<!-- docref-table: bench:caching-hybrid/* -->
<!-- /docref-table -->

<!-- docref-table: bench:caching-redis/* -->
<!-- /docref-table -->

## Key Generation

<!-- docref-table: bench:caching/key-* -->
<!-- /docref-table -->

## Cache Pipeline

<!-- docref-table: bench:caching/pipeline-* -->
<!-- /docref-table -->

## Invalidation

<!-- docref-table: bench:caching/invalidation-* -->
<!-- /docref-table -->

## Pub/Sub

<!-- docref-table: bench:caching/pubsub-* -->
<!-- /docref-table -->

*Auto-generated from benchmark data. See [methodology](../testing/performance-measurement-methodology.md).*
