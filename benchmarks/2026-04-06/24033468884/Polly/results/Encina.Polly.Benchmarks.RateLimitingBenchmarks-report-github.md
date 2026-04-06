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
| RecordFailure                       | Job-MJLQTR | 5              | Default     | 3           |   469.0 ns |  30.31 ns |   4.69 ns |  0.13 |    0.01 |         - |        0.00 |
| GetState                            | Job-MJLQTR | 5              | Default     | 3           |   461.2 ns | 190.99 ns |  29.56 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-MJLQTR | 5              | Default     | 3           | 3,710.1 ns | 776.44 ns | 201.64 ns |  1.05 |    0.08 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-MJLQTR | 5              | Default     | 3           | 3,703.1 ns | 634.03 ns | 164.66 ns |  1.05 |    0.07 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-MJLQTR | 5              | Default     | 3           | 3,252.8 ns | 246.93 ns |  38.21 ns |  0.92 |    0.05 |     440 B |        1.00 |
| RecordSuccess                       | Job-MJLQTR | 5              | Default     | 3           |   487.0 ns | 166.96 ns |  43.36 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-MJLQTR | 5              | Default     | 3           | 3,545.2 ns | 856.77 ns | 222.50 ns |  1.00 |    0.08 |     440 B |        1.00 |
|                                     |            |                |             |             |            |           |           |       |         |           |             |
| RecordFailure                       | MediumRun  | 15             | 2           | 10          |   512.0 ns |  14.84 ns |  21.76 ns |  0.15 |    0.01 |         - |        0.00 |
| GetState                            | MediumRun  | 15             | 2           | 10          |   479.5 ns |  39.44 ns |  56.57 ns |  0.14 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 15             | 2           | 10          | 3,769.9 ns | 270.00 ns | 387.23 ns |  1.14 |    0.12 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 15             | 2           | 10          | 3,678.9 ns |  72.50 ns | 106.27 ns |  1.11 |    0.05 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 15             | 2           | 10          | 3,581.3 ns | 228.00 ns | 319.62 ns |  1.08 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 15             | 2           | 10          |   466.0 ns |  29.49 ns |  41.34 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 15             | 2           | 10          | 3,316.7 ns |  73.99 ns | 101.28 ns |  1.00 |    0.04 |     440 B |        1.00 |
