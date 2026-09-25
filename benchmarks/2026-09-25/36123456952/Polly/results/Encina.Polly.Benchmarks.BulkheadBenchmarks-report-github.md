```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,576.1 ns |  3,646.94 ns | 3,232.92 ns |  7,669.0 ns |  1.18 |    0.71 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  5,697.5 ns |    939.35 ns |   784.40 ns |  5,395.0 ns |  0.89 |    0.38 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    270.6 ns |     52.78 ns |    46.79 ns |    267.0 ns |  0.04 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  5,679.8 ns |  1,778.07 ns | 1,484.77 ns |  5,311.0 ns |  0.89 |    0.43 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 13,366.0 ns |  2,384.99 ns | 1,991.58 ns | 13,926.0 ns |  2.09 |    0.89 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 10,039.8 ns | 60,600.47 ns | 3,321.72 ns |  8,309.5 ns |  1.07 |    0.41 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 11,870.7 ns | 73,000.68 ns | 4,001.41 ns | 11,459.0 ns |  1.26 |    0.48 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    411.8 ns |  6,119.06 ns |   335.41 ns |    258.5 ns |  0.04 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,959.8 ns | 69,731.06 ns | 3,822.19 ns |  6,073.5 ns |  0.84 |    0.41 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 17,422.0 ns | 98,561.99 ns | 5,402.52 ns | 16,693.0 ns |  1.85 |    0.68 |    5528 B |        7.05 |
