```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |------------:|----------:|----------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           |  6,748.5 ns | 167.90 ns | 148.84 ns |  6,732.0 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           |  6,970.4 ns | 109.13 ns |  96.74 ns |  6,984.0 ns |  1.03 |    0.03 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |    511.4 ns |  51.34 ns |  48.02 ns |    501.0 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           |  7,790.1 ns | 361.05 ns | 337.73 ns |  7,909.5 ns |  1.15 |    0.05 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 10,661.0 ns | 426.54 ns | 356.18 ns | 10,640.0 ns |  1.58 |    0.06 |    5528 B |        7.05 |
|                                |            |             |             |             |           |           |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          |  7,189.0 ns | 295.39 ns | 414.10 ns |  6,972.0 ns |  1.00 |    0.08 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          |  7,224.9 ns | 429.05 ns | 601.48 ns |  6,843.0 ns |  1.01 |    0.10 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |    554.2 ns |  22.14 ns |  33.13 ns |    551.0 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          |  7,420.2 ns | 401.88 ns | 576.36 ns |  7,343.2 ns |  1.04 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          | 10,683.6 ns | 132.66 ns | 177.09 ns | 10,665.5 ns |  1.49 |    0.09 |    5528 B |        7.05 |
