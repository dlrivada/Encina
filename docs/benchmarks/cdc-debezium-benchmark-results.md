# Encina.Cdc.Debezium — Benchmark Results

> **Date**: 2026-04-11
> **Package**: `Encina.Cdc.Debezium`
> **Project**: `tests/Encina.BenchmarkTests/Encina.Cdc.Debezium.Benchmarks`
> **Type**: CPU-bound (no Kafka Connect cluster required)

These numbers establish the first baseline for the Encina Debezium CDC connector package. They cover the CPU-bound position round-trip for both the HTTP-backed (`DebeziumCdcPosition`) and the Kafka-backed (`DebeziumKafkaPosition`) variants.

> **Why no end-to-end benchmark?** Debezium itself is not a .NET library — the Encina Debezium package consumes change events emitted by Debezium Server (HTTP) or a Debezium connector running on Kafka Connect. Exercising the full streaming path would require standing up a Kafka Connect cluster + Zookeeper + a source DB + the Debezium connector image, which is prohibitively heavy for a coverage-gate benchmark. The CPU-bound position serialization path is the only Debezium-specific hot path fully owned by `Encina.Cdc.Debezium`.

## Benchmark Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime  : .NET 10.0.5, X64 RyuJIT x86-64-v3
  GC       : Concurrent Workstation
  Job      : ShortRun (IterationCount=3, WarmupCount=3, LaunchCount=1)
```

## Position round-trip — HTTP and Kafka variants

<!-- docref-table: bench:cdc-debezium/* -->
| Method | Mean | Allocated | DocRef | Notes |
|--------|-----:|----------:|--------|-------|
| **DebeziumCdcPositionBenchmarks.CreatePosition** (baseline) | 5 ns | 24 B | `bench:cdc-debezium/position-ctor` | Wraps an opaque offset JSON string |
| **DebeziumCdcPositionBenchmarks.ToBytes** | 34 ns | 152 B | `bench:cdc-debezium/position-tobytes` | `Encoding.UTF8.GetBytes` over the offset JSON |
| **DebeziumCdcPositionBenchmarks.FromBytes** | 37 ns | 304 B | `bench:cdc-debezium/position-frombytes` | `Encoding.UTF8.GetString` + ctor |
| **DebeziumKafkaPositionBenchmarks.ToBytes** | 338 ns | 264 B | `bench:cdc-debezium/kafka-position-tobytes` | `JsonSerializer.SerializeToUtf8Bytes` over `{offsetJson, topic, partition, offset}` |
| **DebeziumKafkaPositionBenchmarks.FromBytes** | 818 ns | 744 B | `bench:cdc-debezium/kafka-position-frombytes` | `JsonDocument.Parse` + typed extractions |
| **DebeziumKafkaPositionBenchmarks.ComparePositions** | 2 ns | 0 B | `bench:cdc-debezium/kafka-position-compare` | Same-topic / same-partition fast path (just a `long.CompareTo`) |
<!-- /docref-table -->

**Key observations**:

- **The HTTP-backed position is ~10x cheaper than the Kafka-backed one** (34 ns vs 338 ns for serialize): the HTTP variant is just a UTF-8 round-trip over an opaque string, while the Kafka variant wraps the offset in a typed envelope that goes through the full `System.Text.Json` serializer.
- **`DebeziumKafkaPosition.FromBytes` at 818 ns is the slowest path** because `JsonDocument.Parse` allocates a pooled reader, walks the tree, and calls `GetProperty(...).GetString()` four times. This is still cheap in absolute terms (< 1 µs), but it's a clear optimization target if the Kafka connector throughput becomes a concern — a `System.Text.Json` source generator would shave most of it.
- **Comparison is allocation-free on the same-topic/same-partition fast path** — which is the common case inside a single-partition consumer — so resume-catch-up logic pays effectively nothing.

## How to reproduce

```pwsh
dotnet run -c Release `
  --project tests/Encina.BenchmarkTests/Encina.Cdc.Debezium.Benchmarks `
  -- --job short --filter "*" --exporters json `
  --artifacts artifacts/performance/cdc-debezium
```

## Related

- Perf manifest: `.github/perf-manifest/Encina.Cdc.Debezium.Benchmarks.json`
- Coverage dashboard: <https://dlrivada.github.io/Encina/performance/>
- Sibling connectors: `docs/benchmarks/cdc-{mysql,postgresql,sqlserver,mongodb}-benchmark-results.md`
