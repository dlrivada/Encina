```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error          | StdDev       | Median        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-------------:|---------------:|-------------:|--------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   509.000 ns |     712.041 ns |   666.044 ns |    91.0000 ns | 0.235 |    0.31 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   381.333 ns |     574.181 ns |   537.089 ns |    60.0000 ns | 0.176 |    0.25 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,196.857 ns |     859.354 ns |   761.795 ns |   947.0000 ns | 0.552 |    0.37 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           |   724.000 ns |     173.372 ns |   144.773 ns |   707.0000 ns | 0.334 |    0.10 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |   701.615 ns |     153.472 ns |   128.156 ns |   690.0000 ns | 0.324 |    0.09 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |     3.077 ns |       7.550 ns |     6.304 ns |     0.0000 ns | 0.001 |    0.00 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,241.357 ns |     412.516 ns |   365.685 ns | 2,264.0000 ns | 1.034 |    0.28 |     440 B |        1.00 |
|                                     |            |                |             |             |              |                |              |               |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   257.333 ns |   3,891.533 ns |   213.308 ns |   211.0000 ns |  0.13 |    0.11 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           | 1,450.667 ns |   6,398.069 ns |   350.700 ns | 1,437.0000 ns |  0.73 |    0.31 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 2,417.000 ns |  43,450.906 ns | 2,381.691 ns | 1,332.0000 ns |  1.21 |    1.17 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 2,746.833 ns |  22,337.919 ns | 1,224.417 ns | 2,282.5000 ns |  1.37 |    0.75 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 6,593.000 ns | 146,521.197 ns | 8,031.321 ns | 3,004.0000 ns |  3.30 |    3.86 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           | 1,021.000 ns |  16,766.577 ns |   919.033 ns | 1,161.0000 ns |  0.51 |    0.46 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,230.000 ns |  15,699.113 ns |   860.521 ns | 2,223.0000 ns |  1.12 |    0.56 |     440 B |        1.00 |
