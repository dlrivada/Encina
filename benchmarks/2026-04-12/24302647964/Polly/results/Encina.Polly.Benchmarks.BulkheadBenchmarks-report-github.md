```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean        | Error       | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           |  7,288.6 ns |   448.08 ns |   397.21 ns |  1.00 |    0.07 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           |  6,653.8 ns |    72.51 ns |    56.61 ns |  0.92 |    0.05 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |    454.7 ns |    57.82 ns |    48.28 ns |  0.06 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           |  7,360.2 ns |   451.75 ns |   422.56 ns |  1.01 |    0.08 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 10,788.8 ns |   295.81 ns |   262.23 ns |  1.48 |    0.08 |    5528 B |        7.05 |
|                                |            |             |             |             |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          |  6,699.3 ns |   179.59 ns |   251.77 ns |  1.00 |    0.05 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          |  6,885.4 ns |   448.64 ns |   657.61 ns |  1.03 |    0.10 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |    398.6 ns |    72.00 ns |   105.54 ns |  0.06 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          |  6,752.3 ns |   207.51 ns |   297.60 ns |  1.01 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          | 11,911.4 ns | 1,169.11 ns | 1,676.70 ns |  1.78 |    0.25 |    5528 B |        7.05 |
