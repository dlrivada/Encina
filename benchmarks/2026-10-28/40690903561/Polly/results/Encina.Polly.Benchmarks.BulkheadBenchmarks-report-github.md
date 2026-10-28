```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  8,058.7 ns |   672.23 ns | 628.80 ns |  1.01 |    0.11 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,666.3 ns |   230.99 ns | 204.77 ns |  0.96 |    0.08 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    510.5 ns |    21.98 ns |  18.35 ns |  0.06 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,741.8 ns |   661.43 ns | 586.34 ns |  0.84 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 11,000.6 ns |   160.78 ns | 134.26 ns |  1.37 |    0.11 |    5528 B |        7.05 |
|                                |            |                |             |             |             |             |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,196.5 ns | 6,801.46 ns | 372.81 ns |  1.00 |    0.06 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,357.7 ns | 4,111.13 ns | 225.34 ns |  1.02 |    0.05 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    547.3 ns | 1,044.89 ns |  57.27 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,096.7 ns | 6,669.58 ns | 365.58 ns |  0.99 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,354.0 ns | 5,324.14 ns | 291.83 ns |  1.58 |    0.08 |    5528 B |        7.05 |
