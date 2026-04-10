```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |-----------:|-------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-MJLQTR | 5              | Default     |   508.0 ns |     61.87 ns |   9.57 ns |  0.14 |    0.01 |         - |        0.00 |
| GetState                            | Job-MJLQTR | 5              | Default     |   581.0 ns |    641.89 ns |  99.33 ns |  0.16 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-MJLQTR | 5              | Default     | 3,996.2 ns |  1,639.69 ns | 425.82 ns |  1.10 |    0.14 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-MJLQTR | 5              | Default     | 4,358.1 ns |    788.30 ns | 204.72 ns |  1.19 |    0.10 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-MJLQTR | 5              | Default     | 3,573.7 ns |    969.33 ns | 251.73 ns |  0.98 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-MJLQTR | 5              | Default     |   475.7 ns |    190.02 ns |  49.35 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-MJLQTR | 5              | Default     | 3,668.6 ns |  1,153.12 ns | 299.46 ns |  1.01 |    0.11 |     440 B |        1.00 |
|                                     |            |                |             |            |              |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           |   638.3 ns |  3,404.81 ns | 186.63 ns |  0.18 |    0.05 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           |   629.8 ns |  3,310.79 ns | 181.48 ns |  0.18 |    0.05 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 4,730.7 ns |  6,604.24 ns | 362.00 ns |  1.32 |    0.12 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 4,363.5 ns |  6,253.13 ns | 342.76 ns |  1.22 |    0.11 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 4,307.3 ns | 15,084.18 ns | 826.81 ns |  1.20 |    0.21 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           |   534.3 ns |    936.20 ns |  51.32 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3,596.8 ns |  4,949.75 ns | 271.31 ns |  1.00 |    0.09 |     440 B |        1.00 |
