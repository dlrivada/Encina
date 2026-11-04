```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 1,631.96 ns |    539.65 ns |   450.64 ns | 1,551.50 ns |  1.07 |    0.39 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 1,632.35 ns |    434.55 ns |   362.87 ns | 1,526.50 ns |  1.07 |    0.35 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    70.21 ns |     51.26 ns |    45.44 ns |    50.00 ns |  0.05 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 1,413.69 ns |    472.57 ns |   394.62 ns | 1,271.00 ns |  0.92 |    0.34 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 6,292.62 ns |    366.35 ns |   305.92 ns | 6,264.00 ns |  4.11 |    1.02 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 3,876.00 ns | 42,554.98 ns | 2,332.58 ns | 3,486.00 ns |  1.31 |    1.04 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 4,338.00 ns | 33,419.06 ns | 1,831.81 ns | 3,651.00 ns |  1.46 |    0.99 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   257.33 ns |  4,387.62 ns |   240.50 ns |   201.00 ns |  0.09 |    0.09 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 4,052.17 ns | 39,785.98 ns | 2,180.80 ns | 3,083.50 ns |  1.37 |    1.03 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 6,184.00 ns | 52,118.58 ns | 2,856.80 ns | 4,802.00 ns |  2.08 |    1.46 |    5528 B |        7.05 |
