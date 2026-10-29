```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error         | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-------------:|--------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  1,715.69 ns |     508.26 ns |   424.42 ns | 1,598.00 ns |  1.05 |    0.34 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  1,829.07 ns |     780.24 ns |   691.66 ns | 1,512.50 ns |  1.12 |    0.48 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |     29.43 ns |      41.15 ns |    36.48 ns |    15.50 ns |  0.02 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  3,002.46 ns |     498.28 ns |   416.09 ns | 2,954.00 ns |  1.84 |    0.47 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  3,731.77 ns |     446.33 ns |   372.70 ns | 3,601.00 ns |  2.29 |    0.54 |    5528 B |        7.05 |
|                                |            |                |             |             |              |               |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  5,157.50 ns |  38,369.95 ns | 2,103.19 ns | 4,786.50 ns |  1.12 |    0.57 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  5,091.33 ns |  42,673.45 ns | 2,339.08 ns | 4,347.00 ns |  1.10 |    0.60 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    235.67 ns |   3,486.43 ns |   191.10 ns |   136.00 ns |  0.05 |    0.04 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  5,440.33 ns |  44,474.95 ns | 2,437.82 ns | 4,906.00 ns |  1.18 |    0.63 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 12,466.00 ns | 154,752.56 ns | 8,482.51 ns | 8,073.00 ns |  2.70 |    1.91 |    5528 B |        7.05 |
