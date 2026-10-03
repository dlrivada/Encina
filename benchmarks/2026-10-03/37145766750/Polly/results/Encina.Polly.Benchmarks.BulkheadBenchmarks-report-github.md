```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev     | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|------------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 3,061.3 ns |    268.7 ns |   209.8 ns | 2,964.5 ns |  1.00 |    0.09 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 1,257.8 ns |    341.0 ns |   266.2 ns | 1,247.0 ns |  0.41 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   706.7 ns |    701.2 ns |   655.9 ns | 1,097.0 ns |  0.23 |    0.21 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 1,476.2 ns |    175.7 ns |   146.7 ns | 1,432.0 ns |  0.48 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 3,932.2 ns |    905.8 ns |   756.3 ns | 3,605.0 ns |  1.29 |    0.25 |    5528 B |        7.05 |
|                                |            |                |             |             |            |             |            |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 5,406.7 ns | 50,106.5 ns | 2,746.5 ns | 4,963.0 ns |  1.20 |    0.78 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 3,875.7 ns | 46,649.5 ns | 2,557.0 ns | 3,064.0 ns |  0.86 |    0.65 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   253.7 ns |  4,227.7 ns |   231.7 ns |   130.0 ns |  0.06 |    0.05 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 3,919.3 ns | 41,267.4 ns | 2,262.0 ns | 3,085.0 ns |  0.87 |    0.61 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 6,326.3 ns | 45,098.2 ns | 2,472.0 ns | 5,368.0 ns |  1.40 |    0.81 |    5528 B |        7.05 |
