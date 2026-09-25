```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   507.9 ns |    44.59 ns |  41.71 ns |  0.15 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   568.9 ns |    35.07 ns |  31.09 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,856.7 ns |   157.08 ns | 139.25 ns |  1.12 |    0.06 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,835.5 ns |   130.33 ns | 121.91 ns |  1.11 |    0.05 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,647.9 ns |   371.57 ns | 329.38 ns |  1.06 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   515.5 ns |    62.71 ns |  55.59 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,461.1 ns |   156.91 ns | 139.10 ns |  1.00 |    0.05 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   437.7 ns |   822.66 ns |  45.09 ns |  0.10 |    0.01 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   491.0 ns |   364.87 ns |  20.00 ns |  0.11 |    0.00 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,824.3 ns | 2,170.19 ns | 118.96 ns |  0.89 |    0.03 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,041.3 ns | 2,001.27 ns | 109.70 ns |  0.94 |    0.03 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,376.0 ns | 3,112.15 ns | 170.59 ns |  0.79 |    0.04 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   561.7 ns | 1,430.75 ns |  78.42 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 4,292.0 ns | 2,148.35 ns | 117.76 ns |  1.00 |    0.03 |     440 B |        1.00 |
