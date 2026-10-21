```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   513.2 ns |    16.96 ns |  15.86 ns |  0.13 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   518.0 ns |    33.06 ns |  30.93 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,606.5 ns |    94.79 ns |  84.03 ns |  0.91 |    0.08 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,791.2 ns |   134.84 ns | 119.53 ns |  0.95 |    0.08 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,557.6 ns |    89.72 ns |  79.53 ns |  0.89 |    0.07 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   459.8 ns |    32.40 ns |  28.72 ns |  0.12 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 4,000.0 ns |   364.04 ns | 322.71 ns |  1.01 |    0.11 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   506.2 ns | 1,435.39 ns |  78.68 ns |  0.15 |    0.02 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   548.0 ns |   555.46 ns |  30.45 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,144.7 ns | 6,849.89 ns | 375.47 ns |  1.21 |    0.12 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,788.8 ns | 2,894.40 ns | 158.65 ns |  1.10 |    0.08 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,615.2 ns | 3,463.92 ns | 189.87 ns |  1.05 |    0.08 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   520.7 ns |   305.46 ns |  16.74 ns |  0.15 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,447.0 ns | 4,795.71 ns | 262.87 ns |  1.00 |    0.09 |     440 B |        1.00 |
