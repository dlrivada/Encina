```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-MJLQTR | 5              | Default     |  7,078.0 ns |   373.1 ns |  57.74 ns |  1.00 |    0.01 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-MJLQTR | 5              | Default     |  7,135.1 ns | 1,013.8 ns | 263.28 ns |  1.01 |    0.03 |     784 B |        1.00 |
| GetMetrics                     | Job-MJLQTR | 5              | Default     |    558.0 ns |   242.6 ns |  63.01 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-MJLQTR | 5              | Default     |  7,098.2 ns |   553.0 ns |  85.58 ns |  1.00 |    0.01 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-MJLQTR | 5              | Default     | 11,016.6 ns | 1,134.7 ns | 294.67 ns |  1.56 |    0.04 |    5528 B |        7.05 |
|                                |            |                |             |             |            |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           |  7,304.0 ns | 2,534.5 ns | 138.92 ns |  1.00 |    0.02 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           |  7,027.0 ns | 3,971.8 ns | 217.71 ns |  0.96 |    0.03 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           |    511.2 ns |   191.6 ns |  10.50 ns |  0.07 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           |  7,150.7 ns | 2,176.5 ns | 119.30 ns |  0.98 |    0.02 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 11,346.2 ns | 5,215.3 ns | 285.87 ns |  1.55 |    0.04 |    5528 B |        7.05 |
