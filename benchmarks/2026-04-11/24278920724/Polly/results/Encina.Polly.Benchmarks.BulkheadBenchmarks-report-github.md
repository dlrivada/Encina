```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean        | Error       | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |------------:|------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           |  6,943.5 ns |    87.97 ns |    68.68 ns |  6,969.0 ns |  1.00 |    0.01 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           |  6,797.4 ns |    75.07 ns |    66.55 ns |  6,788.0 ns |  0.98 |    0.01 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |    720.3 ns |   204.17 ns |   190.98 ns |    816.5 ns |  0.10 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           |  6,836.4 ns |   170.50 ns |   151.15 ns |  6,802.0 ns |  0.98 |    0.02 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 11,066.8 ns |   466.82 ns |   389.81 ns | 10,896.0 ns |  1.59 |    0.06 |    5528 B |        7.05 |
|                                |            |             |             |             |             |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          |  6,906.3 ns |   160.39 ns |   219.55 ns |  6,818.0 ns |  1.00 |    0.04 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          |  7,482.5 ns |   340.37 ns |   477.15 ns |  7,313.5 ns |  1.08 |    0.08 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |    532.5 ns |    27.82 ns |    40.77 ns |    530.0 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          |  6,930.1 ns |   125.76 ns |   176.30 ns |  6,932.0 ns |  1.00 |    0.04 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          | 12,624.1 ns | 1,316.55 ns | 1,888.16 ns | 11,271.5 ns |  1.83 |    0.27 |    5528 B |        7.05 |
