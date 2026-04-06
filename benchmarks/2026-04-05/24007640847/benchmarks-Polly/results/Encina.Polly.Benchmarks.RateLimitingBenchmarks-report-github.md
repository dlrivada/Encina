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
| AcquireAsync_SimpleRateLimiting     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,397.5 ns | 1,073.8 ns | 278.87 ns |  1.01 |    0.10 |     440 B |        1.00 |
| AcquireAsync_WithAdaptiveThrottling | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,416.0 ns |   279.2 ns |  43.20 ns |  1.01 |    0.07 |     440 B |        1.00 |
| RecordSuccess                       | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       526.4 ns |   172.4 ns |  44.77 ns |  0.16 |    0.02 |         - |        0.00 |
| RecordFailure                       | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       428.2 ns |   359.2 ns |  55.59 ns |  0.13 |    0.02 |         - |        0.00 |
| GetState                            | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       773.8 ns | 1,109.5 ns | 288.13 ns |  0.23 |    0.08 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     4,258.2 ns | 2,002.7 ns | 520.08 ns |  1.26 |    0.17 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     4,662.0 ns | 2,551.6 ns | 662.63 ns |  1.38 |    0.21 |     336 B |        0.76 |
|                                     |            |                 |                |             |             |             |                |            |           |       |         |           |             |
| AcquireAsync_SimpleRateLimiting     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,153,226.0 ns |         NA |   0.00 ns |  1.00 |    0.00 |     440 B |        1.00 |
| AcquireAsync_WithAdaptiveThrottling | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,053,059.0 ns |         NA |   0.00 ns |  0.99 |    0.00 |     440 B |        1.00 |
| RecordSuccess                       | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   324,325.0 ns |         NA |   0.00 ns |  0.05 |    0.00 |         - |        0.00 |
| RecordFailure                       | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   299,768.0 ns |         NA |   0.00 ns |  0.04 |    0.00 |         - |        0.00 |
| GetState                            | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   492,829.0 ns |         NA |   0.00 ns |  0.07 |    0.00 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 6,994,179.0 ns |         NA |   0.00 ns |  0.98 |    0.00 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 6,934,978.0 ns |         NA |   0.00 ns |  0.97 |    0.00 |     336 B |        0.76 |
