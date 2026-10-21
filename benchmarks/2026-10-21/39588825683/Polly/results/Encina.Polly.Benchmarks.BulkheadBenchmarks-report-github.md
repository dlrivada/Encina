```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 2,123.64 ns |    723.71 ns |   641.55 ns | 1,947.50 ns |  1.08 |    0.43 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 1,339.83 ns |    348.62 ns |   272.18 ns | 1,237.00 ns |  0.68 |    0.22 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    51.15 ns |     42.18 ns |    35.22 ns |    51.00 ns |  0.03 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 1,562.46 ns |    358.04 ns |   298.98 ns | 1,513.00 ns |  0.79 |    0.25 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 6,406.50 ns |    379.99 ns |   317.31 ns | 6,323.50 ns |  3.25 |    0.85 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 5,715.67 ns | 42,007.32 ns | 2,302.56 ns | 5,228.00 ns |  1.11 |    0.55 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 3,620.67 ns | 44,935.60 ns | 2,463.07 ns | 2,629.00 ns |  0.70 |    0.49 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   292.00 ns |  3,633.07 ns |   199.14 ns |   225.00 ns |  0.06 |    0.04 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 4,212.83 ns | 36,657.21 ns | 2,009.31 ns | 3,164.50 ns |  0.82 |    0.45 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 8,519.67 ns | 45,633.48 ns | 2,501.32 ns | 7,662.00 ns |  1.66 |    0.71 |    5528 B |        7.05 |
