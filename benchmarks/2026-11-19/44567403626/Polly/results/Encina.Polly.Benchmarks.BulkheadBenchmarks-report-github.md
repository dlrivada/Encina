```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev     | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|------------:|-----------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 1,793.6 ns |    605.5 ns |   536.8 ns | 1,647.50 ns |  1.07 |    0.41 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 2,009.7 ns |    472.3 ns |   394.4 ns | 1,928.00 ns |  1.20 |    0.38 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   279.6 ns |    559.2 ns |   495.7 ns |    35.50 ns |  0.17 |    0.30 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 3,145.5 ns |    223.2 ns |   186.4 ns | 3,095.00 ns |  1.88 |    0.47 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 3,429.7 ns |    257.5 ns |   215.0 ns | 3,395.00 ns |  2.05 |    0.52 |    5528 B |        7.05 |
|                                |            |                |             |             |            |             |            |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 4,098.0 ns | 40,319.9 ns | 2,210.1 ns | 3,350.00 ns |  1.19 |    0.77 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 4,756.5 ns | 30,497.9 ns | 1,671.7 ns | 4,505.50 ns |  1.39 |    0.72 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   244.8 ns |  3,780.0 ns |   207.2 ns |   184.50 ns |  0.07 |    0.06 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 4,124.5 ns | 41,188.9 ns | 2,257.7 ns | 3,580.50 ns |  1.20 |    0.78 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 8,147.7 ns | 44,252.4 ns | 2,425.6 ns | 7,156.00 ns |  2.37 |    1.16 |    5528 B |        7.05 |
