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
| TryAcquireAsync_HighLimit      | Job-MJLQTR | 5              | Default     | 3           |  7,091.5 ns | 1,287.41 ns | 199.23 ns |  7,059.0 ns |  1.00 |    0.04 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-MJLQTR | 5              | Default     | 3           |  6,985.4 ns |   743.37 ns | 193.05 ns |  6,913.0 ns |  0.99 |    0.03 |     784 B |        1.00 |
| GetMetrics                     | Job-MJLQTR | 5              | Default     | 3           |    524.0 ns |    30.31 ns |   4.69 ns |    526.0 ns |  0.07 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-MJLQTR | 5              | Default     | 3           |  6,756.8 ns |   768.14 ns | 118.87 ns |  6,757.0 ns |  0.95 |    0.03 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-MJLQTR | 5              | Default     | 3           | 10,562.5 ns |   668.17 ns | 103.40 ns | 10,570.0 ns |  1.49 |    0.04 |    5528 B |        7.05 |
|                                |            |                |             |             |             |             |           |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 15             | 2           | 10          |  6,894.8 ns |   118.07 ns | 157.62 ns |  6,893.5 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 15             | 2           | 10          |  7,334.4 ns |   441.36 ns | 646.93 ns |  6,922.0 ns |  1.06 |    0.10 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 15             | 2           | 10          |    540.0 ns |    15.14 ns |  21.71 ns |    541.0 ns |  0.08 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 15             | 2           | 10          |  7,018.0 ns |   200.40 ns | 287.41 ns |  6,898.0 ns |  1.02 |    0.05 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 15             | 2           | 10          | 10,708.1 ns |    92.45 ns | 132.59 ns | 10,705.5 ns |  1.55 |    0.04 |    5528 B |        7.05 |
