```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,052.9 ns |    406.82 ns |   339.71 ns |  1.00 |    0.06 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,841.5 ns |    105.38 ns |    93.42 ns |  0.97 |    0.04 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    586.6 ns |     53.66 ns |    47.57 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,747.8 ns |    100.27 ns |    83.73 ns |  0.96 |    0.04 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,692.7 ns |     90.68 ns |    75.72 ns |  1.52 |    0.07 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,193.3 ns |  5,872.14 ns |   321.87 ns |  1.00 |    0.06 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,210.0 ns |  2,148.35 ns |   117.76 ns |  1.00 |    0.04 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    566.5 ns |  1,094.62 ns |    60.00 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,063.0 ns |  6,148.19 ns |   337.00 ns |  0.98 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 12,191.0 ns | 37,025.82 ns | 2,029.51 ns |  1.70 |    0.25 |    5528 B |        7.05 |
