```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           |  7,598.6 ns | 564.06 ns | 500.02 ns |  1.00 |    0.09 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           |  7,809.8 ns | 499.97 ns | 467.67 ns |  1.03 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |    567.5 ns |  27.98 ns |  26.17 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           |  7,130.7 ns |  82.87 ns |  73.46 ns |  0.94 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 10,853.7 ns | 154.43 ns | 136.89 ns |  1.43 |    0.10 |    5528 B |        7.05 |
|                                |            |             |             |             |           |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          |  6,891.4 ns |  79.06 ns | 110.84 ns |  1.00 |    0.02 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          |  7,026.6 ns | 126.98 ns | 182.11 ns |  1.02 |    0.03 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |    578.2 ns |  20.28 ns |  29.72 ns |  0.08 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          |  6,994.5 ns |  62.87 ns |  86.06 ns |  1.02 |    0.02 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          | 10,995.8 ns | 105.45 ns | 151.24 ns |  1.60 |    0.03 |    5528 B |        7.05 |
