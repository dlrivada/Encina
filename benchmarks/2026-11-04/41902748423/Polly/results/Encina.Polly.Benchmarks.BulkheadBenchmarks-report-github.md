```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 3,523.08 ns |    371.61 ns |   310.31 ns | 3,395.00 ns |  1.01 |    0.12 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 3,461.07 ns |    588.31 ns |   521.52 ns | 3,300.00 ns |  0.99 |    0.16 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    52.36 ns |     38.09 ns |    33.77 ns |    40.00 ns |  0.01 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 1,859.00 ns |    522.41 ns |   436.24 ns | 1,653.00 ns |  0.53 |    0.13 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 3,844.31 ns |    405.71 ns |   338.79 ns | 3,715.00 ns |  1.10 |    0.13 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 4,078.00 ns | 51,473.38 ns | 2,821.43 ns | 3,370.00 ns |  1.40 |    1.26 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 3,973.00 ns | 42,290.38 ns | 2,318.08 ns | 3,195.00 ns |  1.37 |    1.12 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           | 1,106.67 ns | 17,355.45 ns |   951.31 ns | 1,217.00 ns |  0.38 |    0.39 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 5,578.33 ns | 36,430.25 ns | 1,996.87 ns | 4,957.00 ns |  1.92 |    1.31 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 6,312.83 ns | 47,496.04 ns | 2,603.42 ns | 5,508.50 ns |  2.17 |    1.55 |    5528 B |        7.05 |
