```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median       | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|-------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 3,577.81 ns |    681.84 ns |   569.37 ns | 3,469.500 ns | 1.020 |    0.21 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 3,537.43 ns |    508.38 ns |   450.66 ns | 3,310.000 ns | 1.009 |    0.18 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    32.29 ns |     60.22 ns |    53.38 ns |     5.000 ns | 0.009 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 1,802.19 ns |    397.07 ns |   331.57 ns | 1,682.500 ns | 0.514 |    0.11 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 3,610.46 ns |    304.05 ns |   253.89 ns | 3,531.000 ns | 1.030 |    0.15 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |              |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 4,146.67 ns | 39,105.92 ns | 2,143.53 ns | 3,416.000 ns |  1.18 |    0.72 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 4,112.67 ns | 39,148.35 ns | 2,145.85 ns | 3,595.000 ns |  1.17 |    0.72 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   752.67 ns | 19,043.92 ns | 1,043.86 ns |   155.000 ns |  0.21 |    0.29 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 5,428.33 ns | 29,275.78 ns | 1,604.70 ns | 4,938.000 ns |  1.54 |    0.73 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 7,011.00 ns | 33,170.47 ns | 1,818.19 ns | 7,041.000 ns |  1.99 |    0.91 |    5528 B |        7.05 |
