```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           |  5,643.7 ns | 415.04 ns | 346.58 ns |  1.00 |    0.08 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           |  5,597.0 ns | 214.53 ns | 179.14 ns |  1.00 |    0.06 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |    612.0 ns | 139.70 ns | 123.84 ns |  0.11 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           |  6,464.0 ns | 557.65 ns | 521.63 ns |  1.15 |    0.11 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 10,938.1 ns | 432.79 ns | 383.66 ns |  1.94 |    0.13 |    5528 B |        7.05 |
|                                |            |             |             |             |           |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          |  5,277.0 ns | 142.13 ns | 194.56 ns |  1.00 |    0.05 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          |  5,470.3 ns | 297.25 ns | 416.71 ns |  1.04 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |    395.0 ns |  47.77 ns |  66.96 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          |  5,185.9 ns | 354.02 ns | 484.58 ns |  0.98 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          |  9,351.7 ns | 714.51 ns | 978.03 ns |  1.77 |    0.19 |    5528 B |        7.05 |
