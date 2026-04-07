```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-MJLQTR | 5              | Default     | 3           |   508.0 ns |   167.93 ns |  25.99 ns |  0.14 |    0.02 |         - |        0.00 |
| GetState                            | Job-MJLQTR | 5              | Default     | 3           |   507.5 ns |   110.36 ns |  17.08 ns |  0.14 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-MJLQTR | 5              | Default     | 3           | 3,721.5 ns |   336.21 ns |  52.03 ns |  1.01 |    0.16 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-MJLQTR | 5              | Default     | 3           | 3,687.5 ns |   353.56 ns |  54.71 ns |  1.00 |    0.16 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-MJLQTR | 5              | Default     | 3           | 3,731.4 ns |   424.29 ns | 110.19 ns |  1.01 |    0.16 |     440 B |        1.00 |
| RecordSuccess                       | Job-MJLQTR | 5              | Default     | 3           |   529.5 ns |   311.16 ns |  80.81 ns |  0.14 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-MJLQTR | 5              | Default     | 3           | 3,788.6 ns | 2,567.79 ns | 666.85 ns |  1.02 |    0.23 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |       |         |           |             |
| RecordFailure                       | MediumRun  | 15             | 2           | 10          |   493.4 ns |    42.73 ns |  63.96 ns |  0.14 |    0.02 |         - |        0.00 |
| GetState                            | MediumRun  | 15             | 2           | 10          |   500.5 ns |    29.70 ns |  42.59 ns |  0.15 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 15             | 2           | 10          | 3,695.1 ns |   102.28 ns | 146.69 ns |  1.08 |    0.05 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 15             | 2           | 10          | 3,784.5 ns |    59.19 ns |  82.97 ns |  1.11 |    0.04 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 15             | 2           | 10          | 3,441.3 ns |   161.06 ns | 215.01 ns |  1.01 |    0.07 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 15             | 2           | 10          |   467.9 ns |    20.31 ns |  27.80 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 15             | 2           | 10          | 3,418.1 ns |    58.10 ns |  81.44 ns |  1.00 |    0.03 |     440 B |        1.00 |
