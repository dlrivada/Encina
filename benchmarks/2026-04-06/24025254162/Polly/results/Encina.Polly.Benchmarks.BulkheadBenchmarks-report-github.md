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
| TryAcquireAsync_HighLimit      | Job-MJLQTR | 5              | Default     |  6,813.0 ns |   601.58 ns |  93.09 ns |  1.00 |    0.02 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-MJLQTR | 5              | Default     |  6,935.5 ns |    63.09 ns |   9.76 ns |  1.02 |    0.01 |     784 B |        1.00 |
| GetMetrics                     | Job-MJLQTR | 5              | Default     |    490.2 ns |    72.80 ns |  11.27 ns |  0.07 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-MJLQTR | 5              | Default     |  7,030.4 ns | 1,550.78 ns | 402.73 ns |  1.03 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-MJLQTR | 5              | Default     | 10,850.8 ns | 1,298.63 ns | 200.96 ns |  1.59 |    0.03 |    5528 B |        7.05 |
|                                |            |                |             |             |             |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           |  7,107.0 ns | 2,638.54 ns | 144.63 ns |  1.00 |    0.02 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           |  7,103.7 ns | 2,638.75 ns | 144.64 ns |  1.00 |    0.02 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           |    532.0 ns | 1,837.73 ns | 100.73 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           |  6,816.2 ns | 6,158.20 ns | 337.55 ns |  0.96 |    0.04 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 11,050.0 ns | 5,473.12 ns | 300.00 ns |  1.56 |    0.05 |    5528 B |        7.05 |
