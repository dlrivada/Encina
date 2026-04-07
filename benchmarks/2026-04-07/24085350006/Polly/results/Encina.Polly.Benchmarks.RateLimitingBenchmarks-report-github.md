```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | Mean       | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |-----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-MJLQTR | 5              | Default     |   585.6 ns |   149.4 ns |  38.80 ns |  0.16 |    0.01 |         - |        0.00 |
| GetState                            | Job-MJLQTR | 5              | Default     |   742.0 ns |   764.6 ns | 118.32 ns |  0.20 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-MJLQTR | 5              | Default     | 3,834.8 ns |   371.6 ns |  57.51 ns |  1.06 |    0.02 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-MJLQTR | 5              | Default     | 4,035.0 ns |   314.5 ns |  48.67 ns |  1.11 |    0.02 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-MJLQTR | 5              | Default     | 3,501.0 ns |   123.7 ns |  19.15 ns |  0.96 |    0.02 |     440 B |        1.00 |
| RecordSuccess                       | Job-MJLQTR | 5              | Default     |   446.5 ns |   114.4 ns |  17.71 ns |  0.12 |    0.00 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-MJLQTR | 5              | Default     | 3,635.2 ns |   280.8 ns |  72.91 ns |  1.00 |    0.03 |     440 B |        1.00 |
|                                     |            |                |             |            |            |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           |   570.3 ns | 2,710.3 ns | 148.56 ns |  0.16 |    0.04 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           |   607.7 ns |   899.9 ns |  49.33 ns |  0.17 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3,984.7 ns | 1,635.2 ns |  89.63 ns |  1.09 |    0.04 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3,980.0 ns | 3,150.8 ns | 172.70 ns |  1.09 |    0.05 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3,721.5 ns | 2,413.4 ns | 132.29 ns |  1.02 |    0.04 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           |   542.0 ns | 1,448.1 ns |  79.37 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3,667.0 ns | 2,219.4 ns | 121.66 ns |  1.00 |    0.04 |     440 B |        1.00 |
