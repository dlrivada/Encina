```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,083.4 ns |    592.22 ns |   524.98 ns | 6,030.5 ns |  1.01 |    0.12 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  3,974.9 ns |    373.83 ns |   291.86 ns | 3,959.5 ns |  0.66 |    0.07 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    115.1 ns |     43.33 ns |    38.41 ns |   109.5 ns |  0.02 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  4,698.5 ns |    880.91 ns |   735.60 ns | 4,806.5 ns |  0.78 |    0.13 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  8,139.3 ns |    324.41 ns |   253.28 ns | 8,105.5 ns |  1.35 |    0.12 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  8,511.5 ns | 45,717.47 ns | 2,505.93 ns | 7,704.5 ns |  1.05 |    0.37 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  6,992.8 ns | 39,776.23 ns | 2,180.27 ns | 5,819.5 ns |  0.87 |    0.31 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    438.7 ns |  5,137.17 ns |   281.59 ns |   321.0 ns |  0.05 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,878.0 ns | 40,538.48 ns | 2,222.05 ns | 7,153.0 ns |  0.98 |    0.33 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 10,252.7 ns | 63,378.65 ns | 3,474.00 ns | 9,353.0 ns |  1.27 |    0.48 |    5528 B |        7.05 |
