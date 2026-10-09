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
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   517.0 ns |    28.16 ns |  24.96 ns |  0.13 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   624.6 ns |   204.30 ns | 191.10 ns |  0.16 |    0.05 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,409.4 ns |   412.93 ns | 366.05 ns |  1.11 |    0.12 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,556.8 ns |   127.61 ns | 113.12 ns |  1.14 |    0.08 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 4,204.5 ns |   371.05 ns | 328.93 ns |  1.05 |    0.11 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   728.1 ns |   180.64 ns | 168.97 ns |  0.18 |    0.04 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 4,005.0 ns |   298.49 ns | 264.61 ns |  1.00 |    0.09 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   641.7 ns | 3,601.01 ns | 197.38 ns |  0.19 |    0.05 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   580.3 ns | 1,005.94 ns |  55.14 ns |  0.17 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,986.7 ns | 2,061.65 ns | 113.01 ns |  1.18 |    0.05 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,880.2 ns | 1,196.37 ns |  65.58 ns |  1.15 |    0.04 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,809.2 ns | 7,513.96 ns | 411.87 ns |  1.13 |    0.11 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   628.0 ns | 4,465.32 ns | 244.76 ns |  0.19 |    0.06 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,367.8 ns | 2,122.89 ns | 116.36 ns |  1.00 |    0.04 |     440 B |        1.00 |
