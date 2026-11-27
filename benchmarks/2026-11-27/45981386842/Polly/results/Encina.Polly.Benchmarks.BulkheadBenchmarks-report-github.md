```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  5,226.8 ns |     242.55 ns |   189.37 ns | 5,247.5 ns |  1.00 |    0.05 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  5,332.6 ns |     272.14 ns |   241.25 ns | 5,333.5 ns |  1.02 |    0.06 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    383.8 ns |      22.67 ns |    20.10 ns |   385.5 ns |  0.07 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  5,445.6 ns |     250.98 ns |   222.48 ns | 5,373.0 ns |  1.04 |    0.05 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  8,283.2 ns |     341.48 ns |   302.72 ns | 8,242.0 ns |  1.59 |    0.08 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  5,881.2 ns |   9,284.79 ns |   508.93 ns | 5,687.5 ns |  1.00 |    0.10 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  5,712.3 ns |  10,571.36 ns |   579.45 ns | 5,569.0 ns |  0.98 |    0.11 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    424.3 ns |   1,968.66 ns |   107.91 ns |   401.0 ns |  0.07 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  5,761.7 ns |   7,633.87 ns |   418.44 ns | 5,668.0 ns |  0.98 |    0.09 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 12,134.7 ns | 111,310.64 ns | 6,101.31 ns | 8,753.0 ns |  2.07 |    0.92 |    5528 B |        7.05 |
