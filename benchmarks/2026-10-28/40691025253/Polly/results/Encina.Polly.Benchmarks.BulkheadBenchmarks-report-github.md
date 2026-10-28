```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,616.3 ns |     400.44 ns |   354.98 ns |  6,595.0 ns |  1.00 |    0.07 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,546.4 ns |     372.84 ns |   330.51 ns |  6,545.0 ns |  0.99 |    0.07 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    451.3 ns |      30.26 ns |    26.82 ns |    440.5 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,488.5 ns |     347.88 ns |   290.49 ns |  6,410.0 ns |  0.98 |    0.07 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,649.4 ns |     328.60 ns |   291.30 ns | 10,662.0 ns |  1.61 |    0.09 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,199.7 ns |  17,359.19 ns |   951.52 ns |  7,206.0 ns |  1.01 |    0.17 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,507.3 ns |  24,789.15 ns | 1,358.78 ns |  7,170.0 ns |  1.06 |    0.21 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    618.8 ns |   1,863.48 ns |   102.14 ns |    575.5 ns |  0.09 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 10,582.0 ns | 124,188.69 ns | 6,807.20 ns |  7,210.0 ns |  1.49 |    0.85 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,440.7 ns |  21,100.85 ns | 1,156.61 ns | 11,808.0 ns |  1.61 |    0.23 |    5528 B |        7.05 |
