```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,883.6 ns |    809.89 ns |   717.94 ns |  6,935.5 ns |  1.01 |    0.15 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,777.4 ns |  1,021.92 ns |   905.91 ns |  7,253.5 ns |  1.00 |    0.17 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    288.6 ns |     39.91 ns |    35.38 ns |    296.0 ns |  0.04 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,790.7 ns |    586.11 ns |   519.57 ns |  6,921.0 ns |  1.00 |    0.13 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 12,329.8 ns |  1,607.71 ns | 1,425.19 ns | 11,982.0 ns |  1.81 |    0.28 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  8,181.8 ns | 58,546.47 ns | 3,209.13 ns |  6,837.5 ns |  1.09 |    0.50 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 10,511.8 ns | 60,407.46 ns | 3,311.14 ns |  9,560.5 ns |  1.41 |    0.57 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    438.0 ns |  4,080.36 ns |   223.66 ns |    319.0 ns |  0.06 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  8,764.2 ns | 77,803.33 ns | 4,264.66 ns |  7,361.5 ns |  1.17 |    0.61 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 13,590.8 ns | 80,214.79 ns | 4,396.84 ns | 12,037.5 ns |  1.82 |    0.74 |    5528 B |        7.05 |
