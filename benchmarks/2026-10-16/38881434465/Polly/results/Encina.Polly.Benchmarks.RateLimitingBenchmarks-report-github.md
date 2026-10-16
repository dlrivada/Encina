```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   333.7 ns |     62.81 ns |  55.68 ns |   306.0 ns |  0.14 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   414.7 ns |     86.19 ns |  76.41 ns |   400.5 ns |  0.18 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,706.5 ns |    123.72 ns |  96.59 ns | 2,699.0 ns |  1.16 |    0.05 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,816.0 ns |    238.34 ns | 222.94 ns | 2,753.0 ns |  1.21 |    0.10 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,312.0 ns |    126.71 ns | 105.81 ns | 2,293.0 ns |  0.99 |    0.05 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   306.3 ns |    171.17 ns | 151.74 ns |   259.5 ns |  0.13 |    0.06 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,331.5 ns |     81.20 ns |  67.80 ns | 2,313.0 ns |  1.00 |    0.04 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   417.7 ns |  1,004.79 ns |  55.08 ns |   391.0 ns |  0.14 |    0.02 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   367.7 ns |  1,856.05 ns | 101.74 ns |   314.0 ns |  0.12 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,289.8 ns | 13,095.80 ns | 717.82 ns | 3,239.5 ns |  1.09 |    0.25 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,264.8 ns |  5,927.68 ns | 324.92 ns | 3,354.5 ns |  1.08 |    0.17 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,130.7 ns | 12,659.54 ns | 693.91 ns | 3,204.0 ns |  1.04 |    0.24 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   534.2 ns |  4,698.61 ns | 257.55 ns |   390.5 ns |  0.18 |    0.08 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,067.3 ns |  9,312.78 ns | 510.46 ns | 2,813.0 ns |  1.02 |    0.20 |     440 B |        1.00 |
