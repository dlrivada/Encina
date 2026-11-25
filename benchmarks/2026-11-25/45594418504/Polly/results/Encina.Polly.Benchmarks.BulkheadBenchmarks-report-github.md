```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,524.0 ns |    559.6 ns |   496.06 ns |  1.00 |    0.09 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,403.8 ns |    541.9 ns |   452.53 ns |  0.99 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    653.9 ns |    180.0 ns |   168.35 ns |  0.09 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,575.1 ns |    431.7 ns |   403.83 ns |  1.01 |    0.08 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 14,036.7 ns |  1,892.8 ns | 1,580.58 ns |  1.87 |    0.24 |    5528 B |        7.05 |
|                                |            |                |             |             |             |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,333.3 ns |  4,795.0 ns |   262.83 ns |  1.00 |    0.04 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,201.0 ns |  7,182.3 ns |   393.68 ns |  0.98 |    0.06 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    547.5 ns |  1,385.4 ns |    75.94 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,395.5 ns | 12,878.0 ns |   705.89 ns |  1.01 |    0.09 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,052.7 ns |  7,818.3 ns |   428.55 ns |  1.51 |    0.07 |    5528 B |        7.05 |
