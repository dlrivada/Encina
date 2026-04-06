```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| AcquireAsync_SimpleRateLimiting     | Job-MJLQTR | 5              | Default     | 3,249.0 ns | 1,136.44 ns | 175.87 ns |  1.00 |    0.07 |     440 B |        1.00 |
| AcquireAsync_WithAdaptiveThrottling | Job-MJLQTR | 5              | Default     | 3,455.8 ns |   378.76 ns |  98.36 ns |  1.07 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | Job-MJLQTR | 5              | Default     |   549.4 ns |    49.81 ns |  12.93 ns |  0.17 |    0.01 |         - |        0.00 |
| RecordFailure                       | Job-MJLQTR | 5              | Default     |   513.8 ns |   146.44 ns |  22.66 ns |  0.16 |    0.01 |         - |        0.00 |
| GetState                            | Job-MJLQTR | 5              | Default     |   421.0 ns |   211.08 ns |  32.66 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-MJLQTR | 5              | Default     | 4,318.2 ns | 1,045.37 ns | 161.77 ns |  1.33 |    0.08 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-MJLQTR | 5              | Default     | 3,874.2 ns | 1,072.36 ns | 278.49 ns |  1.20 |    0.10 |     336 B |        0.76 |
|                                     |            |                |             |            |             |           |       |         |           |             |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3,479.0 ns | 3,753.82 ns | 205.76 ns |  1.00 |    0.07 |     440 B |        1.00 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3,490.3 ns | 2,034.26 ns | 111.50 ns |  1.01 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           |   461.3 ns |   844.02 ns |  46.26 ns |  0.13 |    0.01 |         - |        0.00 |
| RecordFailure                       | ShortRun   | 3              | 1           |   597.3 ns |   919.51 ns |  50.40 ns |  0.17 |    0.02 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           |   635.3 ns |   936.20 ns |  51.32 ns |  0.18 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3,857.7 ns | 3,704.88 ns | 203.08 ns |  1.11 |    0.07 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 4,274.7 ns | 5,038.85 ns | 276.20 ns |  1.23 |    0.09 |     336 B |        0.76 |
