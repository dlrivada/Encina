```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-MJLQTR | 5              | Default     | 3           |   520.4 ns | 113.92 ns |  29.59 ns |  0.15 |    0.01 |         - |        0.00 |
| GetState                            | Job-MJLQTR | 5              | Default     | 3           |   508.6 ns | 335.54 ns |  87.14 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-MJLQTR | 5              | Default     | 3           | 3,852.8 ns | 678.47 ns | 104.99 ns |  1.11 |    0.05 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-MJLQTR | 5              | Default     | 3           | 3,942.0 ns | 200.91 ns |  31.09 ns |  1.14 |    0.04 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-MJLQTR | 5              | Default     | 3           | 3,634.5 ns | 630.47 ns |  97.57 ns |  1.05 |    0.04 |     440 B |        1.00 |
| RecordSuccess                       | Job-MJLQTR | 5              | Default     | 3           |   723.8 ns | 676.46 ns | 175.67 ns |  0.21 |    0.05 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-MJLQTR | 5              | Default     | 3           | 3,470.5 ns | 849.46 ns | 131.45 ns |  1.00 |    0.05 |     440 B |        1.00 |
|                                     |            |                |             |             |            |           |           |       |         |           |             |
| RecordFailure                       | MediumRun  | 15             | 2           | 10          |   448.8 ns |  27.83 ns |  38.10 ns |  0.14 |    0.01 |         - |        0.00 |
| GetState                            | MediumRun  | 15             | 2           | 10          |   527.2 ns |  46.24 ns |  67.77 ns |  0.16 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 15             | 2           | 10          | 4,092.6 ns | 304.13 ns | 426.35 ns |  1.23 |    0.13 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 15             | 2           | 10          | 3,633.9 ns | 107.10 ns | 150.13 ns |  1.09 |    0.05 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 15             | 2           | 10          | 3,325.1 ns |  56.12 ns |  80.49 ns |  1.00 |    0.04 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 15             | 2           | 10          |   497.6 ns |  45.81 ns |  68.57 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 15             | 2           | 10          | 3,324.3 ns |  68.27 ns |  93.45 ns |  1.00 |    0.04 |     440 B |        1.00 |
