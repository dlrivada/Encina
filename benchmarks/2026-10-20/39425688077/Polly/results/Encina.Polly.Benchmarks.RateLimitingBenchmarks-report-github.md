```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   269.0 ns |     70.83 ns |  55.30 ns |   255.0 ns |  0.09 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   462.2 ns |     95.76 ns |  89.58 ns |   465.5 ns |  0.16 |    0.04 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,323.0 ns |    217.71 ns | 192.99 ns | 3,305.0 ns |  1.14 |    0.15 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,555.7 ns |    359.85 ns | 336.60 ns | 3,471.0 ns |  1.22 |    0.18 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,843.9 ns |    497.48 ns | 465.35 ns | 2,735.0 ns |  0.98 |    0.19 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   380.4 ns |     40.12 ns |  31.32 ns |   380.5 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,957.3 ns |    409.97 ns | 383.49 ns | 2,765.0 ns |  1.01 |    0.17 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   796.2 ns |  5,449.66 ns | 298.71 ns |   876.5 ns |  0.24 |    0.08 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   568.8 ns |  2,009.57 ns | 110.15 ns |   515.5 ns |  0.17 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,756.0 ns | 12,745.70 ns | 698.64 ns | 3,686.0 ns |  1.11 |    0.22 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,019.2 ns | 13,252.04 ns | 726.39 ns | 4,216.5 ns |  1.19 |    0.23 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,355.2 ns |  5,940.58 ns | 325.62 ns | 3,365.5 ns |  1.00 |    0.14 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   463.7 ns |  5,034.42 ns | 275.95 ns |   330.0 ns |  0.14 |    0.07 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,403.5 ns |  7,345.15 ns | 402.61 ns | 3,610.5 ns |  1.01 |    0.15 |     440 B |        1.00 |
