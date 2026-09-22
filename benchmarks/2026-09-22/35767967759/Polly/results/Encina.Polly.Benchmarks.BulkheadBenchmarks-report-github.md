```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  3,950.2 ns |    403.50 ns |   336.94 ns |  3,827.5 ns |  1.01 |    0.11 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  5,917.5 ns |    488.02 ns |   407.52 ns |  5,989.0 ns |  1.51 |    0.15 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    216.1 ns |     27.25 ns |    21.28 ns |    211.0 ns |  0.06 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  4,536.7 ns |  1,344.97 ns | 1,050.06 ns |  4,011.0 ns |  1.16 |    0.27 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  8,099.9 ns |    241.79 ns |   201.90 ns |  8,080.0 ns |  2.06 |    0.16 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  6,909.0 ns | 58,307.32 ns | 3,196.02 ns |  5,899.0 ns |  1.14 |    0.63 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,259.2 ns | 52,513.51 ns | 2,878.44 ns |  6,690.5 ns |  1.20 |    0.61 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    419.2 ns |  6,696.75 ns |   367.07 ns |    248.5 ns |  0.07 |    0.06 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  6,607.3 ns | 52,568.35 ns | 2,881.45 ns |  5,846.0 ns |  1.09 |    0.58 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,507.5 ns | 59,153.89 ns | 3,242.42 ns | 10,095.5 ns |  1.90 |    0.83 |    5528 B |        7.05 |
