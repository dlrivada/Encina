```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                              | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | Mean           | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |---------------- |--------------- |------------ |------------ |------------ |---------------:|------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       461.2 ns |   285.13 ns |  44.12 ns |  0.13 |    0.01 |         - |        0.00 |
| GetState                            | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       472.0 ns |   222.21 ns |  57.71 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,925.5 ns |   710.57 ns | 109.96 ns |  1.11 |    0.04 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,823.1 ns |   670.13 ns | 174.03 ns |  1.08 |    0.05 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     4,006.5 ns | 2,475.64 ns | 642.92 ns |  1.13 |    0.17 |     440 B |        1.00 |
| RecordSuccess                       | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       548.4 ns |    70.72 ns |  18.37 ns |  0.15 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,546.2 ns |   341.06 ns |  88.57 ns |  1.00 |    0.03 |     440 B |        1.00 |
|                                     |            |                 |                |             |             |             |                |             |           |       |         |           |             |
| RecordFailure                       | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   304,501.0 ns |          NA |   0.00 ns |  0.04 |    0.00 |         - |        0.00 |
| GetState                            | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   460,734.0 ns |          NA |   0.00 ns |  0.06 |    0.00 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,150,705.0 ns |          NA |   0.00 ns |  1.01 |    0.00 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,106,904.0 ns |          NA |   0.00 ns |  1.00 |    0.00 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,171,294.0 ns |          NA |   0.00 ns |  1.01 |    0.00 |     440 B |        1.00 |
| RecordSuccess                       | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   326,853.0 ns |          NA |   0.00 ns |  0.05 |    0.00 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,095,773.0 ns |          NA |   0.00 ns |  1.00 |    0.00 |     440 B |        1.00 |
