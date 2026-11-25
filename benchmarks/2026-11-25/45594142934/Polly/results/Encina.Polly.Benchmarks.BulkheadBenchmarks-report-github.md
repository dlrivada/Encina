```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 2,188.50 ns |     617.02 ns |   546.97 ns | 2,102.50 ns |  1.05 |    0.34 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 2,280.79 ns |     879.05 ns |   779.25 ns | 2,238.50 ns |  1.09 |    0.43 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    72.54 ns |      61.00 ns |    50.94 ns |    60.00 ns |  0.03 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 2,144.71 ns |   1,398.85 ns | 1,240.04 ns | 1,597.00 ns |  1.03 |    0.62 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 5,883.86 ns |     918.17 ns |   813.93 ns | 5,808.50 ns |  2.82 |    0.69 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 7,529.17 ns | 140,007.84 ns | 7,674.30 ns | 3,489.50 ns |  1.80 |    2.08 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 4,515.00 ns |  41,096.41 ns | 2,252.63 ns | 3,851.00 ns |  1.08 |    0.82 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   288.67 ns |   3,812.04 ns |   208.95 ns |   245.00 ns |  0.07 |    0.06 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 4,380.33 ns |  36,472.10 ns | 1,999.16 ns | 3,816.00 ns |  1.04 |    0.76 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 9,232.33 ns |  60,071.91 ns | 3,292.74 ns | 8,358.00 ns |  2.20 |    1.49 |    5528 B |        7.05 |
