```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           |  7,491.1 ns | 831.09 ns | 736.74 ns |  1.01 |    0.13 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           |  6,917.6 ns |  95.83 ns |  84.95 ns |  0.93 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |    600.4 ns |  34.79 ns |  32.55 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           |  6,882.0 ns | 208.45 ns | 174.07 ns |  0.93 |    0.09 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 10,994.1 ns | 344.08 ns | 268.64 ns |  1.48 |    0.14 |    5528 B |        7.05 |
|                                |            |             |             |             |           |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          |  6,898.4 ns |  91.66 ns | 128.49 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          |  7,066.8 ns |  84.24 ns | 115.31 ns |  1.02 |    0.02 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |    534.9 ns |  80.90 ns | 118.58 ns |  0.08 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          |  6,890.4 ns | 155.62 ns | 223.18 ns |  1.00 |    0.04 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          | 10,906.1 ns | 108.11 ns | 147.98 ns |  1.58 |    0.04 |    5528 B |        7.05 |
