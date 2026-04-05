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
| TryAcquireAsync_HighLimit      | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |      7,405.5 ns | 1,327.1 ns | 344.64 ns |  1.00 |    0.06 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |      7,084.3 ns | 1,519.8 ns | 394.68 ns |  0.96 |    0.06 |     784 B |        1.00 |
| GetMetrics                     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |        536.1 ns |   380.2 ns |  98.74 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |      7,081.5 ns |   797.3 ns | 207.05 ns |  0.96 |    0.05 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     11,102.2 ns | 5,467.2 ns | 846.05 ns |  1.50 |    0.12 |    5528 B |        7.05 |
|                                |            |                 |                |             |             |             |                 |            |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 11,350,073.0 ns |         NA |   0.00 ns |  1.00 |    0.00 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 11,689,628.0 ns |         NA |   0.00 ns |  1.03 |    0.00 |     784 B |        1.00 |
| GetMetrics                     | Dry        | Default         | 1              | 1           | ColdStart   | 1           |    790,919.0 ns |         NA |   0.00 ns |  0.07 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 11,151,211.0 ns |         NA |   0.00 ns |  0.98 |    0.00 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 12,544,318.0 ns |         NA |   0.00 ns |  1.11 |    0.00 |    5528 B |        7.05 |
