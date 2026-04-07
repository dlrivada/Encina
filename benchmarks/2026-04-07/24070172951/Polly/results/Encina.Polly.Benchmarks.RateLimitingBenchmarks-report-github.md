```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.91GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | Mean       | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |-----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-MJLQTR | 5              | Default     |   527.4 ns |   111.9 ns |  29.06 ns |  0.15 |    0.01 |         - |        0.00 |
| GetState                            | Job-MJLQTR | 5              | Default     |   516.2 ns |   108.4 ns |  28.15 ns |  0.15 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-MJLQTR | 5              | Default     | 4,543.0 ns |   938.7 ns | 145.26 ns |  1.31 |    0.05 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-MJLQTR | 5              | Default     | 3,744.8 ns | 1,368.0 ns | 211.69 ns |  1.08 |    0.06 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-MJLQTR | 5              | Default     | 3,299.0 ns |   513.3 ns |  79.43 ns |  0.95 |    0.03 |     440 B |        1.00 |
| RecordSuccess                       | Job-MJLQTR | 5              | Default     |   433.5 ns |   122.3 ns |  18.93 ns |  0.12 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-MJLQTR | 5              | Default     | 3,473.3 ns |   351.2 ns |  91.19 ns |  1.00 |    0.03 |     440 B |        1.00 |
|                                     |            |                |             |            |            |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           |   478.8 ns |   459.1 ns |  25.17 ns |  0.13 |    0.01 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           |   544.7 ns | 1,178.1 ns |  64.58 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 5,020.8 ns | 4,296.8 ns | 235.52 ns |  1.39 |    0.13 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 4,137.0 ns | 1,672.1 ns |  91.65 ns |  1.15 |    0.09 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3,449.8 ns | 3,434.2 ns | 188.24 ns |  0.96 |    0.09 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           |   471.7 ns |   737.3 ns |  40.41 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3,633.3 ns | 6,255.0 ns | 342.86 ns |  1.01 |    0.12 |     440 B |        1.00 |
