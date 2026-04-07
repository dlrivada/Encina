```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                              | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | Mean           | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |---------------- |--------------- |------------ |------------ |------------ |---------------:|----------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       507.6 ns | 166.35 ns |  43.20 ns |  0.14 |    0.01 |         - |        0.00 |
| GetState                            | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       543.2 ns |  59.08 ns |   9.14 ns |  0.15 |    0.00 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,840.3 ns | 560.78 ns | 145.63 ns |  1.07 |    0.05 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,845.1 ns | 394.35 ns | 102.41 ns |  1.07 |    0.04 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,539.7 ns | 521.85 ns | 135.52 ns |  0.99 |    0.04 |     440 B |        1.00 |
| RecordSuccess                       | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       522.4 ns | 180.34 ns |  46.83 ns |  0.15 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,596.0 ns | 402.02 ns | 104.40 ns |  1.00 |    0.04 |     440 B |        1.00 |
|                                     |            |                 |                |             |             |             |                |           |           |       |         |           |             |
| RecordFailure                       | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   294,357.0 ns |        NA |   0.00 ns |  0.04 |    0.00 |         - |        0.00 |
| GetState                            | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   459,576.0 ns |        NA |   0.00 ns |  0.06 |    0.00 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 6,895,742.0 ns |        NA |   0.00 ns |  0.95 |    0.00 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,003,152.0 ns |        NA |   0.00 ns |  0.96 |    0.00 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,069,285.0 ns |        NA |   0.00 ns |  0.97 |    0.00 |     440 B |        1.00 |
| RecordSuccess                       | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   319,505.0 ns |        NA |   0.00 ns |  0.04 |    0.00 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,284,706.0 ns |        NA |   0.00 ns |  1.00 |    0.00 |     440 B |        1.00 |
