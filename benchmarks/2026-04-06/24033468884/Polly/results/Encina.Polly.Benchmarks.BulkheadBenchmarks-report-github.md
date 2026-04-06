```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev    | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|----------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-MJLQTR | 5              | Default     | 3           |  7,086.4 ns |   851.87 ns | 221.23 ns |  7,102.0 ns |  1.00 |    0.04 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-MJLQTR | 5              | Default     | 3           |  7,215.0 ns |   809.92 ns | 210.33 ns |  7,143.0 ns |  1.02 |    0.04 |     784 B |        1.00 |
| GetMetrics                     | Job-MJLQTR | 5              | Default     | 3           |    554.0 ns |    82.25 ns |  12.73 ns |    551.0 ns |  0.08 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-MJLQTR | 5              | Default     | 3           |  6,950.0 ns |   309.34 ns |  47.87 ns |  6,952.5 ns |  0.98 |    0.03 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-MJLQTR | 5              | Default     | 3           | 10,948.8 ns | 1,603.48 ns | 416.42 ns | 10,830.0 ns |  1.55 |    0.07 |    5528 B |        7.05 |
|                                |            |                |             |             |             |             |           |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 15             | 2           | 10          |  7,414.8 ns |   376.51 ns | 563.55 ns |  7,309.0 ns |  1.01 |    0.11 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 15             | 2           | 10          |  7,189.1 ns |   378.06 ns | 542.20 ns |  6,894.0 ns |  0.97 |    0.10 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 15             | 2           | 10          |    558.0 ns |    23.34 ns |  33.47 ns |    552.0 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 15             | 2           | 10          |  7,474.1 ns |   338.03 ns | 495.48 ns |  7,274.0 ns |  1.01 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 15             | 2           | 10          | 10,708.4 ns |    94.28 ns | 129.05 ns | 10,705.0 ns |  1.45 |    0.11 |    5528 B |        7.05 |
