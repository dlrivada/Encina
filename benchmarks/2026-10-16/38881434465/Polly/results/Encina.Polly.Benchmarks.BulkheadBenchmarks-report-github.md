```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,367.8 ns |    838.35 ns |   700.06 ns |  7,073.0 ns |  1.01 |    0.12 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,169.2 ns |    232.85 ns |   194.44 ns |  7,122.0 ns |  0.98 |    0.08 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    528.2 ns |     39.78 ns |    33.22 ns |    521.0 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,060.6 ns |    124.58 ns |   110.44 ns |  7,062.5 ns |  0.97 |    0.08 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 11,280.5 ns |    490.68 ns |   458.98 ns | 11,060.0 ns |  1.54 |    0.14 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,240.0 ns |  6,486.84 ns |   355.57 ns |  7,143.0 ns |  1.00 |    0.06 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 10,240.8 ns | 99,646.95 ns | 5,461.99 ns |  7,198.5 ns |  1.42 |    0.66 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    834.3 ns |  2,374.02 ns |   130.13 ns |    841.0 ns |  0.12 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,291.0 ns |  5,841.33 ns |   320.18 ns |  7,354.0 ns |  1.01 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,464.7 ns |  5,490.73 ns |   300.97 ns | 11,301.0 ns |  1.59 |    0.08 |    5528 B |        7.05 |
