```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.39GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 3,271.58 ns |    377.46 ns |   315.20 ns | 3,244.50 ns |  1.01 |    0.13 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 1,548.62 ns |    385.46 ns |   321.88 ns | 1,602.00 ns |  0.48 |    0.11 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    94.00 ns |     89.03 ns |    78.93 ns |    44.50 ns |  0.03 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 1,500.77 ns |    476.76 ns |   398.12 ns | 1,292.00 ns |  0.46 |    0.13 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 6,042.64 ns |    949.15 ns |   841.40 ns | 6,104.00 ns |  1.86 |    0.30 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 3,946.33 ns | 40,468.80 ns | 2,218.23 ns | 3,005.00 ns |  1.20 |    0.78 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 4,020.50 ns | 48,239.04 ns | 2,644.14 ns | 3,349.50 ns |  1.22 |    0.88 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   611.33 ns | 15,396.91 ns |   843.96 ns |   190.00 ns |  0.19 |    0.25 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 3,780.67 ns | 39,836.23 ns | 2,183.56 ns | 3,490.00 ns |  1.15 |    0.76 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 8,351.00 ns | 40,182.42 ns | 2,202.53 ns | 7,717.00 ns |  2.54 |    1.17 |    5528 B |        7.05 |
