```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   607.8 ns |    215.68 ns | 180.10 ns |  0.18 |    0.05 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   541.6 ns |    252.49 ns | 223.83 ns |  0.16 |    0.06 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,171.6 ns |    487.18 ns | 455.71 ns |  1.20 |    0.13 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,073.0 ns |    444.00 ns | 393.59 ns |  1.17 |    0.11 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,493.7 ns |    272.30 ns | 241.39 ns |  1.01 |    0.07 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   490.3 ns |     98.01 ns |  91.68 ns |  0.14 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,470.5 ns |    132.27 ns | 103.27 ns |  1.00 |    0.04 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   530.7 ns |    173.40 ns |   9.50 ns |  0.11 |    0.01 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   480.8 ns |    191.63 ns |  10.50 ns |  0.10 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,384.7 ns |  6,140.97 ns | 336.61 ns |  0.88 |    0.07 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 5,060.0 ns | 12,351.16 ns | 677.01 ns |  1.01 |    0.13 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 4,024.2 ns |  9,686.10 ns | 530.93 ns |  0.81 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   574.0 ns |  2,191.30 ns | 120.11 ns |  0.11 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 5,005.7 ns |  5,653.92 ns | 309.91 ns |  1.00 |    0.08 |     440 B |        1.00 |
