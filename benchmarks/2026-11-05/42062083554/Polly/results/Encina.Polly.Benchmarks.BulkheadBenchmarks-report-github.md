```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  5,371.2 ns |    648.56 ns |   541.58 ns |  5,334.5 ns |  1.01 |    0.14 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  4,774.3 ns |    436.20 ns |   364.24 ns |  4,754.5 ns |  0.90 |    0.11 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    305.9 ns |     31.06 ns |    25.94 ns |    304.0 ns |  0.06 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  5,716.8 ns |    696.26 ns |   617.22 ns |  5,825.0 ns |  1.07 |    0.15 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  9,392.6 ns |    426.29 ns |   355.97 ns |  9,376.5 ns |  1.77 |    0.19 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  8,761.2 ns | 83,632.65 ns | 4,584.19 ns |  7,340.5 ns |  1.19 |    0.75 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  8,725.3 ns | 76,272.56 ns | 4,180.76 ns |  7,077.0 ns |  1.18 |    0.71 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    483.2 ns |  6,496.57 ns |   356.10 ns |    355.5 ns |  0.07 |    0.05 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,720.7 ns | 52,081.97 ns | 2,854.79 ns |  6,942.0 ns |  1.04 |    0.55 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 12,925.8 ns | 81,087.16 ns | 4,444.66 ns | 11,157.5 ns |  1.75 |    0.90 |    5528 B |        7.05 |
