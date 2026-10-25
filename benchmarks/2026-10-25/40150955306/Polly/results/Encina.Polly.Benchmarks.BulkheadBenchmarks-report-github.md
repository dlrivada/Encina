```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |------------:|----------:|----------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           | 3,263.62 ns | 282.13 ns | 235.59 ns | 3,245.00 ns |  1.00 |    0.10 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           | 1,332.46 ns | 325.48 ns | 271.79 ns | 1,207.00 ns |  0.41 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |    90.86 ns |  37.32 ns |  33.09 ns |    90.00 ns |  0.03 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           | 2,529.08 ns | 837.24 ns | 699.14 ns | 2,844.00 ns |  0.78 |    0.21 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 3,780.71 ns | 652.23 ns | 578.19 ns | 3,601.00 ns |  1.16 |    0.19 |    5528 B |        7.05 |
|                                |            |             |             |             |           |           |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          | 1,415.29 ns | 256.53 ns | 351.14 ns | 1,339.25 ns |  1.06 |    0.36 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          | 2,052.23 ns | 385.24 ns | 527.32 ns | 2,015.50 ns |  1.53 |    0.53 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |    43.07 ns |  22.47 ns |  31.49 ns |    31.00 ns |  0.03 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          | 1,560.83 ns | 233.87 ns | 320.13 ns | 1,484.25 ns |  1.17 |    0.36 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          | 4,230.61 ns | 600.88 ns | 861.76 ns | 3,795.50 ns |  3.16 |    0.97 |    5528 B |        7.05 |
