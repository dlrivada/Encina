```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error         | StdDev       | Median        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-------------:|--------------:|-------------:|--------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |     3.077 ns |     10.237 ns |     8.549 ns |     0.0000 ns | 0.004 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |    98.071 ns |     56.499 ns |    50.085 ns |    95.0000 ns | 0.125 |    0.07 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,027.071 ns |    384.810 ns |   341.124 ns |   891.0000 ns | 1.309 |    0.50 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,433.000 ns |    258.351 ns |   229.021 ns | 2,379.0000 ns | 3.101 |    0.68 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |   698.667 ns |     84.430 ns |    65.918 ns |   696.0000 ns | 0.890 |    0.19 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    17.846 ns |      9.910 ns |     8.275 ns |    20.0000 ns | 0.023 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           |   822.143 ns |    227.020 ns |   201.247 ns |   746.0000 ns | 1.048 |    0.33 |     440 B |        1.00 |
|                                     |            |                |             |             |              |               |              |               |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   247.333 ns |  3,252.827 ns |   178.298 ns |   211.0000 ns |  0.16 |    0.13 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   335.333 ns |  3,253.339 ns |   178.326 ns |   275.0000 ns |  0.22 |    0.14 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 2,296.333 ns | 32,763.107 ns | 1,795.856 ns | 1,772.0000 ns |  1.52 |    1.26 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 2,049.667 ns | 21,736.508 ns | 1,191.451 ns | 1,392.0000 ns |  1.36 |    0.92 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 1,655.667 ns | 22,357.050 ns | 1,225.465 ns | 1,061.0000 ns |  1.10 |    0.88 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   293.667 ns |  4,074.536 ns |   223.339 ns |   190.0000 ns |  0.19 |    0.16 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 1,829.333 ns | 19,019.038 ns | 1,042.498 ns | 1,422.0000 ns |  1.21 |    0.81 |     440 B |        1.00 |
