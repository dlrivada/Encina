```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error         | StdDev       | Median       | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-------------:|--------------:|-------------:|-------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |    89.143 ns |     65.106 ns |    57.715 ns |    79.500 ns | 0.048 |    0.04 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |    13.231 ns |     21.772 ns |    18.180 ns |    10.000 ns | 0.007 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,342.077 ns |    199.375 ns |   166.487 ns | 2,393.000 ns | 1.258 |    0.54 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,470.929 ns |    274.953 ns |   243.739 ns | 2,489.000 ns | 1.327 |    0.58 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |   615.167 ns |    152.599 ns |   119.139 ns |   591.000 ns | 0.330 |    0.15 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |     5.462 ns |      8.630 ns |     7.207 ns |     5.000 ns | 0.003 |    0.00 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,041.714 ns |    517.546 ns |   458.791 ns | 2,193.000 ns | 1.097 |    0.53 |     440 B |        1.00 |
|                                     |            |                |             |             |              |               |              |              |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   781.333 ns | 14,621.548 ns |   801.456 ns |   511.000 ns |  0.54 |    0.60 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   254.833 ns |  4,489.970 ns |   246.110 ns |   194.500 ns |  0.18 |    0.19 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 2,151.667 ns | 20,842.464 ns | 1,142.446 ns | 2,098.000 ns |  1.50 |    1.06 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,600.333 ns | 16,685.301 ns |   914.578 ns | 3,230.000 ns |  2.51 |    1.38 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 1,854.333 ns | 22,173.324 ns | 1,215.395 ns | 1,407.000 ns |  1.29 |    1.03 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   268.500 ns |  4,144.458 ns |   227.172 ns |   185.500 ns |  0.19 |    0.18 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 1,826.333 ns | 19,900.100 ns | 1,090.792 ns | 1,562.000 ns |  1.27 |    0.96 |     440 B |        1.00 |
