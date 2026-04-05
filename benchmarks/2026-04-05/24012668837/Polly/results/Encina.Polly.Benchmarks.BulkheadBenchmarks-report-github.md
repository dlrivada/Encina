```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                         | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | Mean            | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |---------------- |--------------- |------------ |------------ |------------ |----------------:|------------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |      7,049.0 ns | 1,204.89 ns | 186.46 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |      7,990.5 ns |   634.25 ns |  98.15 ns |  1.13 |    0.03 |     784 B |        1.00 |
| GetMetrics                     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |        582.1 ns |    42.41 ns |  11.01 ns |  0.08 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |      6,441.5 ns | 1,636.65 ns | 253.27 ns |  0.91 |    0.04 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     10,604.5 ns | 1,032.58 ns | 159.79 ns |  1.51 |    0.04 |    5528 B |        7.05 |
|                                |            |                 |                |             |             |             |                 |             |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10,921,217.0 ns |          NA |   0.00 ns |  1.00 |    0.00 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10,746,508.0 ns |          NA |   0.00 ns |  0.98 |    0.00 |     784 B |        1.00 |
| GetMetrics                     | Dry        | Default         | 1              | 1           | ColdStart   | 1           |    566,224.0 ns |          NA |   0.00 ns |  0.05 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10,463,667.0 ns |          NA |   0.00 ns |  0.96 |    0.00 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 11,654,746.0 ns |          NA |   0.00 ns |  1.07 |    0.00 |    5528 B |        7.05 |
