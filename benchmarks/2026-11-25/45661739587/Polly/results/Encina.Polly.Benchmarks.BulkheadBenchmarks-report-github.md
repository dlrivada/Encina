```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev     | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|------------:|-----------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 3,689.1 ns |    805.5 ns |   672.6 ns | 3,420.00 ns |  1.03 |    0.23 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 1,912.7 ns |    444.6 ns |   371.3 ns | 1,908.00 ns |  0.53 |    0.13 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   454.8 ns |    712.9 ns |   666.9 ns |   100.00 ns |  0.13 |    0.18 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 2,255.0 ns |    801.6 ns |   710.6 ns | 2,073.50 ns |  0.63 |    0.21 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 6,634.4 ns |    488.8 ns |   408.2 ns | 6,545.00 ns |  1.84 |    0.29 |    5528 B |        7.05 |
|                                |            |                |             |             |            |             |            |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 5,400.0 ns | 33,445.5 ns | 1,833.3 ns | 5,123.00 ns |  1.08 |    0.45 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 5,405.0 ns | 37,741.8 ns | 2,068.8 ns | 4,828.00 ns |  1.08 |    0.48 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   202.0 ns |  4,488.0 ns |   246.0 ns |    65.00 ns |  0.04 |    0.05 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 4,864.0 ns | 61,668.5 ns | 3,380.3 ns | 3,555.00 ns |  0.97 |    0.67 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 6,894.0 ns | 48,117.9 ns | 2,637.5 ns | 6,180.00 ns |  1.38 |    0.62 |    5528 B |        7.05 |
