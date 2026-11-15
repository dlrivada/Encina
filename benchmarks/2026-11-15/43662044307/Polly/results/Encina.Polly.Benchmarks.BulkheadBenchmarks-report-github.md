```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean        | Error       | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           |  7,206.2 ns |   157.01 ns |   139.18 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           |  7,036.8 ns |   194.60 ns |   172.50 ns |  0.98 |    0.03 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |    547.1 ns |    63.47 ns |    56.26 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           |  6,888.4 ns |   150.68 ns |   125.83 ns |  0.96 |    0.02 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 11,807.8 ns | 2,063.25 ns | 1,829.02 ns |  1.64 |    0.25 |    5528 B |        7.05 |
|                                |            |             |             |             |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          |  7,005.6 ns |   141.47 ns |   202.89 ns |  1.00 |    0.04 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          |  6,825.8 ns |    88.49 ns |   124.05 ns |  0.98 |    0.03 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |    518.6 ns |    25.20 ns |    36.14 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          |  7,361.8 ns |   473.46 ns |   648.07 ns |  1.05 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          | 10,822.6 ns |   126.02 ns |   172.50 ns |  1.55 |    0.05 |    5528 B |        7.05 |
