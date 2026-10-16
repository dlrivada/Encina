```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 2,951.08 ns |    358.21 ns |   299.12 ns | 2,930.00 ns |  1.01 |    0.14 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 1,590.36 ns |    575.28 ns |   509.97 ns | 1,382.00 ns |  0.54 |    0.18 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    44.36 ns |     52.59 ns |    46.62 ns |    20.00 ns |  0.02 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 1,196.00 ns |    346.21 ns |   289.10 ns | 1,106.00 ns |  0.41 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 6,402.27 ns |    328.52 ns |   274.33 ns | 6,399.50 ns |  2.19 |    0.24 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 3,456.50 ns | 41,099.22 ns | 2,252.79 ns | 2,839.50 ns |  1.33 |    1.09 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 5,209.17 ns | 40,425.73 ns | 2,215.87 ns | 4,842.50 ns |  2.00 |    1.34 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   237.33 ns |  4,112.34 ns |   225.41 ns |   161.00 ns |  0.09 |    0.10 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 4,727.33 ns | 38,438.10 ns | 2,106.92 ns | 4,156.00 ns |  1.82 |    1.24 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 5,822.67 ns | 53,070.79 ns | 2,908.99 ns | 4,758.00 ns |  2.24 |    1.61 |    5528 B |        7.05 |
