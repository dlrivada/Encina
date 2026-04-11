```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           |  6,673.0 ns | 584.91 ns | 547.12 ns |  1.01 |    0.11 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           |  7,124.6 ns | 372.07 ns | 329.83 ns |  1.07 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |    540.3 ns | 204.68 ns | 191.46 ns |  0.08 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           |  7,649.8 ns | 404.93 ns | 378.77 ns |  1.15 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 10,826.4 ns | 449.16 ns | 350.67 ns |  1.63 |    0.13 |    5528 B |        7.05 |
|                                |            |             |             |             |           |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          |  6,732.7 ns | 503.46 ns | 705.78 ns |  1.01 |    0.15 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          |  6,785.3 ns | 304.63 ns | 427.06 ns |  1.02 |    0.12 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |    496.6 ns |  62.74 ns |  91.96 ns |  0.07 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          |  6,753.0 ns | 283.41 ns | 397.31 ns |  1.01 |    0.12 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          | 13,080.8 ns | 286.75 ns | 401.98 ns |  1.96 |    0.21 |    5528 B |        7.05 |
