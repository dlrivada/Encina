```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                              | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | Mean           | Error    | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |---------------- |--------------- |------------ |------------ |------------ |---------------:|---------:|----------:|------:|--------:|----------:|------------:|
| AcquireAsync_SimpleRateLimiting     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,388.2 ns | 505.8 ns |  78.27 ns |  1.00 |    0.03 |     440 B |        1.00 |
| AcquireAsync_WithAdaptiveThrottling | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,499.8 ns | 397.8 ns | 103.32 ns |  1.03 |    0.03 |     440 B |        1.00 |
| RecordSuccess                       | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       468.0 ns | 381.8 ns |  59.09 ns |  0.14 |    0.02 |         - |        0.00 |
| RecordFailure                       | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       484.4 ns | 119.4 ns |  31.00 ns |  0.14 |    0.01 |         - |        0.00 |
| GetState                            | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       461.2 ns | 220.1 ns |  34.06 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,895.4 ns | 809.5 ns | 210.22 ns |  1.15 |    0.06 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,959.1 ns | 613.6 ns | 159.35 ns |  1.17 |    0.05 |     336 B |        0.76 |
|                                     |            |                 |                |             |             |             |                |          |           |       |         |           |             |
| AcquireAsync_SimpleRateLimiting     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,127,387.0 ns |       NA |   0.00 ns |  1.00 |    0.00 |     440 B |        1.00 |
| AcquireAsync_WithAdaptiveThrottling | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,204,500.0 ns |       NA |   0.00 ns |  1.01 |    0.00 |     440 B |        1.00 |
| RecordSuccess                       | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   325,474.0 ns |       NA |   0.00 ns |  0.05 |    0.00 |         - |        0.00 |
| RecordFailure                       | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   302,381.0 ns |       NA |   0.00 ns |  0.04 |    0.00 |         - |        0.00 |
| GetState                            | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   490,801.0 ns |       NA |   0.00 ns |  0.07 |    0.00 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,064,350.0 ns |       NA |   0.00 ns |  0.99 |    0.00 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,063,680.0 ns |       NA |   0.00 ns |  0.99 |    0.00 |     336 B |        0.76 |
