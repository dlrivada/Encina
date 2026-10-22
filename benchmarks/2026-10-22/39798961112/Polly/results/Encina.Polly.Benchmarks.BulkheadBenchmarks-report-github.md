```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean        | Error       | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |------------:|------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           |  6,992.1 ns |   106.62 ns |    89.03 ns |  7,014.0 ns |  1.00 |    0.02 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           |  6,841.1 ns |   106.12 ns |    94.07 ns |  6,828.0 ns |  0.98 |    0.02 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |    511.6 ns |    27.82 ns |    24.66 ns |    515.5 ns |  0.07 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           |  6,734.0 ns |   220.80 ns |   195.74 ns |  6,691.5 ns |  0.96 |    0.03 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 10,516.0 ns |   317.79 ns |   281.71 ns | 10,474.0 ns |  1.50 |    0.04 |    5528 B |        7.05 |
|                                |            |             |             |             |             |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          |  6,898.8 ns |   142.69 ns |   209.16 ns |  6,822.0 ns |  1.00 |    0.04 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          |  7,286.6 ns |   334.05 ns |   468.29 ns |  7,167.5 ns |  1.06 |    0.07 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |    405.2 ns |    17.60 ns |    24.67 ns |    401.0 ns |  0.06 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          |  7,334.2 ns |   437.10 ns |   612.76 ns |  7,373.0 ns |  1.06 |    0.09 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          | 12,532.8 ns | 1,298.51 ns | 1,903.34 ns | 11,530.0 ns |  1.82 |    0.28 |    5528 B |        7.05 |
