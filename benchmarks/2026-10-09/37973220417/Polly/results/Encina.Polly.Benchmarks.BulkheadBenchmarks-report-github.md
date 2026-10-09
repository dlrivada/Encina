```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.05GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,386.8 ns |  1,133.49 ns |   884.96 ns |  6,242.5 ns |  1.02 |    0.18 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,403.8 ns |  1,386.96 ns | 1,229.50 ns |  6,224.5 ns |  1.02 |    0.22 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    210.4 ns |     31.44 ns |    27.87 ns |    198.5 ns |  0.03 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  4,391.2 ns |    269.39 ns |   224.95 ns |  4,278.0 ns |  0.70 |    0.09 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,449.0 ns |    702.38 ns |   548.37 ns | 10,366.5 ns |  1.66 |    0.21 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  8,836.0 ns | 55,434.54 ns | 3,038.55 ns |  8,707.0 ns |  1.09 |    0.48 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 10,283.5 ns | 61,169.30 ns | 3,352.90 ns |  9,230.5 ns |  1.27 |    0.54 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |  1,225.7 ns | 28,313.60 ns | 1,551.96 ns |    374.0 ns |  0.15 |    0.18 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 10,272.8 ns | 69,060.40 ns | 3,785.43 ns |  9,021.5 ns |  1.26 |    0.57 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 13,259.5 ns | 83,738.33 ns | 4,589.98 ns | 11,723.5 ns |  1.63 |    0.72 |    5528 B |        7.05 |
