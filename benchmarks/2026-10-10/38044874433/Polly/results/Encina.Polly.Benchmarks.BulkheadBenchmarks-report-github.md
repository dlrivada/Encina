```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 2,080.7 ns |    562.57 ns |   469.77 ns | 1,877.50 ns |  1.04 |    0.30 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 2,313.8 ns |    963.61 ns |   804.66 ns | 2,008.50 ns |  1.16 |    0.45 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   116.5 ns |     88.20 ns |    78.19 ns |    95.50 ns |  0.06 |    0.04 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 2,849.0 ns |    599.72 ns |   500.79 ns | 2,985.00 ns |  1.42 |    0.35 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 4,454.9 ns |    584.45 ns |   488.04 ns | 4,332.00 ns |  2.23 |    0.47 |    5528 B |        7.05 |
|                                |            |                |             |             |            |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 4,748.5 ns | 44,238.86 ns | 2,424.88 ns | 4,951.50 ns |  1.25 |    0.91 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 4,121.3 ns | 41,644.95 ns | 2,282.70 ns | 3,370.00 ns |  1.09 |    0.82 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   249.0 ns |  5,219.01 ns |   286.07 ns |   126.00 ns |  0.07 |    0.08 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 4,323.3 ns | 49,621.71 ns | 2,719.93 ns | 3,636.00 ns |  1.14 |    0.93 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 6,076.0 ns | 54,920.25 ns | 3,010.36 ns | 5,208.00 ns |  1.60 |    1.15 |    5528 B |        7.05 |
