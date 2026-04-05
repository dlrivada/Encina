```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                              | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | Mean           | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |---------------- |--------------- |------------ |------------ |------------ |---------------:|------------:|----------:|------:|--------:|----------:|------------:|
| AcquireAsync_SimpleRateLimiting     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,424.2 ns | 1,092.51 ns | 169.07 ns |  1.00 |    0.06 |     440 B |        1.00 |
| AcquireAsync_WithAdaptiveThrottling | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,504.5 ns |   877.66 ns | 227.93 ns |  1.03 |    0.08 |     440 B |        1.00 |
| RecordSuccess                       | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       606.2 ns |    63.62 ns |   9.84 ns |  0.18 |    0.01 |         - |        0.00 |
| RecordFailure                       | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       492.0 ns |   242.62 ns |  63.01 ns |  0.14 |    0.02 |         - |        0.00 |
| GetState                            | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       469.2 ns |    98.05 ns |  15.17 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     4,566.4 ns | 3,230.72 ns | 839.01 ns |  1.34 |    0.23 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     4,837.3 ns | 2,492.25 ns | 647.23 ns |  1.42 |    0.19 |     336 B |        0.76 |
|                                     |            |                 |                |             |             |             |                |             |           |       |         |           |             |
| AcquireAsync_SimpleRateLimiting     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,688,012.0 ns |          NA |   0.00 ns |  1.00 |    0.00 |     440 B |        1.00 |
| AcquireAsync_WithAdaptiveThrottling | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,677,873.0 ns |          NA |   0.00 ns |  1.00 |    0.00 |     440 B |        1.00 |
| RecordSuccess                       | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   333,214.0 ns |          NA |   0.00 ns |  0.04 |    0.00 |         - |        0.00 |
| RecordFailure                       | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   350,366.0 ns |          NA |   0.00 ns |  0.05 |    0.00 |         - |        0.00 |
| GetState                            | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   500,106.0 ns |          NA |   0.00 ns |  0.07 |    0.00 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,572,696.0 ns |          NA |   0.00 ns |  0.99 |    0.00 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,919,095.0 ns |          NA |   0.00 ns |  1.03 |    0.00 |     336 B |        0.76 |
