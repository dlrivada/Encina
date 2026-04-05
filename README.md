# perf-raw — Raw performance measurement archive

This orphan branch stores uncompressed raw output from BenchmarkDotNet
and NBomber runs, one directory per CI workflow run. It exists to
enable unlimited historical recalculation of performance metrics
when the measurement methodology evolves, bypassing the 90-day
retention of GitHub Actions artifacts.

See ADR-025 (Performance Measurement Infrastructure) and
`docs/testing/performance-measurement-methodology.md` for the full
rationale.

## Layout

```
benchmarks/
  <YYYY-MM-DD>/
    <runId>/
      metadata.json
      <Module>/BenchmarkDotNet.Artifacts/results/*.json
load-tests/
  <YYYY-MM-DD>/
    <runId>/
      metadata.json
      nbomber-<stamp>/
      metrics-<stamp>.csv
```

## Policy

- Never rewritten; append-only.
- Unbounded growth accepted per ADR-025 §2 (zero-cost storage on
  public repositories).
- Compression, pruning, or migration to a different store can be
  introduced later without breaking `perf-recalculate.cs`, which
  iterates whatever directories happen to exist.
