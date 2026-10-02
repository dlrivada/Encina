```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,725.7 ns |     445.10 ns |   394.57 ns |  6,695.0 ns |  1.00 |    0.08 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,806.2 ns |     306.94 ns |   272.09 ns |  6,794.0 ns |  1.02 |    0.07 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    292.3 ns |      64.22 ns |    56.93 ns |    286.0 ns |  0.04 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,625.7 ns |     430.53 ns |   381.65 ns |  6,565.0 ns |  0.99 |    0.08 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,752.6 ns |     370.79 ns |   309.63 ns | 10,725.0 ns |  1.60 |    0.10 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,411.3 ns |  10,642.99 ns |   583.38 ns |  7,301.0 ns |  1.00 |    0.10 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 10,661.0 ns | 109,996.52 ns | 6,029.28 ns |  7,326.0 ns |  1.44 |    0.72 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    498.8 ns |   2,161.19 ns |   118.46 ns |    435.5 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,066.7 ns |  15,386.66 ns |   843.39 ns |  7,140.0 ns |  0.96 |    0.12 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,783.3 ns |   7,593.43 ns |   416.22 ns | 11,617.0 ns |  1.60 |    0.12 |    5528 B |        7.05 |
