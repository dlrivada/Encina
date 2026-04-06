```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                         | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | Mean            | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |---------------- |--------------- |------------ |------------ |------------ |----------------:|-----------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |      6,997.2 ns | 1,081.4 ns | 167.34 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |      8,042.0 ns | 4,868.1 ns | 753.35 ns |  1.15 |    0.10 |     784 B |        1.00 |
| GetMetrics                     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |        463.2 ns |   210.4 ns |  54.63 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |      7,776.8 ns | 2,336.5 ns | 606.78 ns |  1.11 |    0.08 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     10,995.2 ns | 1,139.9 ns | 176.40 ns |  1.57 |    0.04 |    5528 B |        7.05 |
|                                |            |                 |                |             |             |             |                 |            |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10,648,066.0 ns |         NA |   0.00 ns |  1.00 |    0.00 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10,606,528.0 ns |         NA |   0.00 ns |  1.00 |    0.00 |     784 B |        1.00 |
| GetMetrics                     | Dry        | Default         | 1              | 1           | ColdStart   | 1           |    594,728.0 ns |         NA |   0.00 ns |  0.06 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10,278,296.0 ns |         NA |   0.00 ns |  0.97 |    0.00 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 11,286,235.0 ns |         NA |   0.00 ns |  1.06 |    0.00 |    5528 B |        7.05 |
