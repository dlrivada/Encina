```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-MJLQTR | 5              | Default     |  6,992.4 ns |  1,585.7 ns | 411.79 ns |  1.00 |    0.07 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-MJLQTR | 5              | Default     |  7,134.3 ns |    527.3 ns | 136.94 ns |  1.02 |    0.06 |     784 B |        1.00 |
| GetMetrics                     | Job-MJLQTR | 5              | Default     |    600.1 ns |    270.5 ns |  70.24 ns |  0.09 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-MJLQTR | 5              | Default     |  7,077.5 ns |  1,222.2 ns | 189.14 ns |  1.01 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-MJLQTR | 5              | Default     | 10,695.2 ns |  1,418.4 ns | 219.49 ns |  1.53 |    0.08 |    5528 B |        7.05 |
|                                |            |                |             |             |             |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           |  7,341.0 ns |  9,956.1 ns | 545.73 ns |  1.00 |    0.09 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           |  7,405.5 ns |  9,517.7 ns | 521.70 ns |  1.01 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           |    541.7 ns |  1,120.2 ns |  61.40 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           |  7,984.3 ns |  3,525.4 ns | 193.24 ns |  1.09 |    0.07 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 11,214.2 ns | 10,734.7 ns | 588.41 ns |  1.53 |    0.12 |    5528 B |        7.05 |
