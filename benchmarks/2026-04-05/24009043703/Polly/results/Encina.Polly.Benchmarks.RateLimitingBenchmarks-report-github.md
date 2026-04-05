```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                              | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | Mean           | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |---------------- |--------------- |------------ |------------ |------------ |---------------:|-----------:|----------:|------:|--------:|----------:|------------:|
| AcquireAsync_SimpleRateLimiting     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     4,162.5 ns | 1,876.3 ns | 290.36 ns |  1.00 |    0.09 |     440 B |        1.00 |
| AcquireAsync_WithAdaptiveThrottling | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,545.9 ns |   601.9 ns | 156.31 ns |  0.86 |    0.07 |     440 B |        1.00 |
| RecordSuccess                       | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       531.6 ns |   259.7 ns |  67.46 ns |  0.13 |    0.02 |         - |        0.00 |
| RecordFailure                       | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       501.2 ns |   191.7 ns |  29.67 ns |  0.12 |    0.01 |         - |        0.00 |
| GetState                            | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       476.8 ns |   532.9 ns |  82.46 ns |  0.11 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,753.4 ns |   947.8 ns | 246.15 ns |  0.91 |    0.08 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     4,159.8 ns | 2,457.0 ns | 380.23 ns |  1.00 |    0.11 |     336 B |        0.76 |
|                                     |            |                 |                |             |             |             |                |            |           |       |         |           |             |
| AcquireAsync_SimpleRateLimiting     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,208,315.0 ns |         NA |   0.00 ns |  1.00 |    0.00 |     440 B |        1.00 |
| AcquireAsync_WithAdaptiveThrottling | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,315,445.0 ns |         NA |   0.00 ns |  1.01 |    0.00 |     440 B |        1.00 |
| RecordSuccess                       | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   305,432.0 ns |         NA |   0.00 ns |  0.04 |    0.00 |         - |        0.00 |
| RecordFailure                       | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   302,406.0 ns |         NA |   0.00 ns |  0.04 |    0.00 |         - |        0.00 |
| GetState                            | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   486,051.0 ns |         NA |   0.00 ns |  0.07 |    0.00 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,095,964.0 ns |         NA |   0.00 ns |  0.98 |    0.00 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,106,063.0 ns |         NA |   0.00 ns |  0.99 |    0.00 |     336 B |        0.76 |
