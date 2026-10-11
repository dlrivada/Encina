```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean        | Error       | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |------------:|------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           |  5,818.9 ns |   975.72 ns |   864.95 ns |  5,522.5 ns |  1.02 |    0.20 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           |  7,107.3 ns |   425.09 ns |   354.97 ns |  6,975.5 ns |  1.24 |    0.17 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |    287.9 ns |    41.43 ns |    36.72 ns |    285.0 ns |  0.05 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           |  4,162.4 ns |   342.20 ns |   285.75 ns |  4,203.0 ns |  0.73 |    0.11 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 14,093.8 ns |   510.12 ns |   425.97 ns | 14,086.0 ns |  2.47 |    0.33 |    5528 B |        7.05 |
|                                |            |             |             |             |             |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          |  6,062.4 ns |   593.26 ns |   791.99 ns |  6,315.0 ns |  1.02 |    0.19 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          |  5,750.6 ns |   984.56 ns | 1,380.22 ns |  5,823.0 ns |  0.96 |    0.26 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |    947.1 ns |   580.76 ns |   832.90 ns |    313.8 ns |  0.16 |    0.14 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          |  7,107.9 ns |   496.02 ns |   695.35 ns |  7,100.0 ns |  1.19 |    0.20 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          | 11,570.0 ns | 1,462.82 ns | 2,050.66 ns | 12,017.0 ns |  1.94 |    0.43 |    5528 B |        7.05 |
