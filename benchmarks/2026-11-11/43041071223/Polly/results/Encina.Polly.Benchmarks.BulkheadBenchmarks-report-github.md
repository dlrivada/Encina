```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error         | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|--------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 5,385.1 ns |     629.65 ns |   588.97 ns | 5,328.0 ns |  1.01 |    0.15 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 5,437.9 ns |     229.22 ns |   203.20 ns | 5,388.5 ns |  1.02 |    0.11 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   351.4 ns |      32.16 ns |    28.51 ns |   345.5 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 5,007.1 ns |     201.90 ns |   178.98 ns | 4,998.0 ns |  0.94 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 8,170.4 ns |     267.05 ns |   236.73 ns | 8,162.5 ns |  1.53 |    0.16 |    5528 B |        7.05 |
|                                |            |                |             |             |            |               |             |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 6,089.7 ns |   5,449.66 ns |   298.71 ns | 6,170.0 ns |  1.00 |    0.06 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 6,109.2 ns |   9,971.85 ns |   546.59 ns | 6,138.5 ns |  1.00 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   310.5 ns |   2,385.68 ns |   130.77 ns |   250.5 ns |  0.05 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 9,469.2 ns | 111,922.76 ns | 6,134.86 ns | 6,124.5 ns |  1.56 |    0.88 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 9,313.8 ns |   8,078.91 ns |   442.83 ns | 9,153.5 ns |  1.53 |    0.09 |    5528 B |        7.05 |
