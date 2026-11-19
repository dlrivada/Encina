```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|--------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 1,733.96 ns |    478.42 ns |   399.51 ns | 1,581.5000 ns | 1.044 |    0.31 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 1,641.62 ns |    425.61 ns |   355.40 ns | 1,462.0000 ns | 0.988 |    0.29 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    15.38 ns |     26.15 ns |    21.84 ns |     0.0000 ns | 0.009 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 1,630.75 ns |    396.17 ns |   309.31 ns | 1,547.0000 ns | 0.982 |    0.26 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 3,732.31 ns |    400.95 ns |   334.81 ns | 3,541.0000 ns | 2.247 |    0.48 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |               |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 6,548.33 ns | 79,705.91 ns | 4,368.95 ns | 4,061.0000 ns |  1.27 |    0.95 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 5,221.33 ns | 53,271.83 ns | 2,920.01 ns | 3,766.0000 ns |  1.01 |    0.68 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           | 1,263.67 ns | 18,946.30 ns | 1,038.51 ns | 1,347.0000 ns |  0.25 |    0.21 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 4,451.67 ns | 39,132.27 ns | 2,144.97 ns | 3,630.0000 ns |  0.87 |    0.53 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 6,982.67 ns | 55,852.48 ns | 3,061.46 ns | 6,195.0000 ns |  1.36 |    0.79 |    5528 B |        7.05 |
