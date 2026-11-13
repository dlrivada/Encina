```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  8,659.8 ns |    773.32 ns |   685.53 ns |  1.01 |    0.11 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,163.1 ns |    604.17 ns |   565.15 ns |  0.83 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    441.3 ns |     85.20 ns |    79.70 ns |  0.05 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,181.2 ns |    227.57 ns |   177.67 ns |  0.83 |    0.07 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 11,197.1 ns |    238.76 ns |   199.38 ns |  1.30 |    0.10 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,693.8 ns |  6,063.13 ns |   332.34 ns |  1.00 |    0.05 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  9,057.2 ns | 25,646.28 ns | 1,405.76 ns |  1.18 |    0.16 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    742.0 ns |  4,435.18 ns |   243.11 ns |  0.10 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,360.3 ns |  5,310.19 ns |   291.07 ns |  0.96 |    0.05 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,741.3 ns | 20,117.67 ns | 1,102.72 ns |  1.53 |    0.14 |    5528 B |        7.05 |
