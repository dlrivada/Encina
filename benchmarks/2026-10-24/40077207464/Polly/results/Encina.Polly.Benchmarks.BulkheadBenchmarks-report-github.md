```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 3,372.83 ns |    248.78 ns |   194.23 ns | 3,375.00 ns | 1.003 |    0.08 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 1,992.43 ns |    742.35 ns |   658.07 ns | 1,803.00 ns | 0.593 |    0.19 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    27.67 ns |     16.64 ns |    12.99 ns |    20.50 ns | 0.008 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 3,555.43 ns |    562.92 ns |   499.01 ns | 3,450.50 ns | 1.057 |    0.16 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 7,202.15 ns |    787.43 ns |   657.54 ns | 6,916.00 ns | 2.142 |    0.22 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 5,429.50 ns | 37,122.72 ns | 2,034.82 ns | 5,132.50 ns |  1.10 |    0.51 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 5,025.83 ns | 63,003.37 ns | 3,453.43 ns | 3,990.50 ns |  1.02 |    0.71 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   293.67 ns |  3,931.95 ns |   215.52 ns |   290.00 ns |  0.06 |    0.04 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 4,842.33 ns | 61,678.78 ns | 3,380.82 ns | 3,851.00 ns |  0.98 |    0.69 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 9,891.00 ns | 29,509.54 ns | 1,617.52 ns | 9,744.00 ns |  2.00 |    0.71 |    5528 B |        7.05 |
