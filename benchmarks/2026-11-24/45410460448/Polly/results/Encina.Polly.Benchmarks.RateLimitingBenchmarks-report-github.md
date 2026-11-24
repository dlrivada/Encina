```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.88GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|--------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   128.21 ns |    157.07 ns |   139.24 ns |    66.5000 ns |  0.11 |    0.12 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |    30.73 ns |     54.35 ns |    45.38 ns |     0.0000 ns |  0.03 |    0.04 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,618.14 ns |    522.26 ns |   462.97 ns | 1,455.0000 ns |  1.44 |    0.51 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,480.07 ns |    712.57 ns |   631.68 ns | 1,279.5000 ns |  1.31 |    0.62 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 1,132.27 ns |    343.93 ns |   287.20 ns | 1,017.5000 ns |  1.01 |    0.33 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    89.43 ns |    125.74 ns |   111.46 ns |    37.0000 ns |  0.08 |    0.10 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 1,184.23 ns |    356.89 ns |   298.02 ns | 1,052.0000 ns |  1.05 |    0.34 |     440 B |        1.00 |
|                                     |            |                |             |             |             |              |             |               |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   235.33 ns |  4,924.63 ns |   269.94 ns |   176.0000 ns |  0.12 |    0.17 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   270.33 ns |  4,065.79 ns |   222.86 ns |   181.0000 ns |  0.14 |    0.15 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,592.00 ns | 43,285.89 ns | 2,372.65 ns | 2,579.0000 ns |  1.82 |    1.79 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 2,609.00 ns | 23,838.43 ns | 1,306.66 ns | 2,142.0000 ns |  1.32 |    1.16 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,900.00 ns | 53,388.53 ns | 2,926.41 ns | 3,583.0000 ns |  1.98 |    2.08 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   187.00 ns |  5,274.52 ns |   289.11 ns |    41.0000 ns |  0.09 |    0.17 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,369.33 ns | 53,006.14 ns | 2,905.45 ns | 2,439.0000 ns |  1.71 |    1.95 |     440 B |        1.00 |
