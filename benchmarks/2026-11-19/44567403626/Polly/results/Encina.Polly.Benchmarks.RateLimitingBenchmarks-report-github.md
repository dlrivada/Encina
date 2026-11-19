```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   180.7 ns |    101.87 ns |    90.31 ns |   147.50 ns |  0.05 |    0.03 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   123.8 ns |     65.26 ns |    54.49 ns |   127.00 ns |  0.04 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,441.7 ns |    352.45 ns |   294.31 ns | 2,398.00 ns |  0.73 |    0.28 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,738.7 ns |    208.15 ns |   173.81 ns | 1,673.50 ns |  0.52 |    0.20 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 1,872.6 ns |    201.14 ns |   167.96 ns | 1,829.50 ns |  0.56 |    0.21 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   112.9 ns |     66.24 ns |    58.72 ns |    86.50 ns |  0.03 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,674.4 ns |  1,116.70 ns | 1,044.57 ns | 3,862.50 ns |  1.11 |    0.51 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           | 1,817.2 ns | 16,777.93 ns |   919.66 ns | 2,274.50 ns |  0.42 |    0.20 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   399.7 ns |  6,735.18 ns |   369.18 ns |   251.00 ns |  0.09 |    0.08 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,122.7 ns | 53,198.86 ns | 2,916.01 ns | 3,011.00 ns |  0.96 |    0.62 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 2,683.7 ns | 26,945.74 ns | 1,476.99 ns | 2,085.00 ns |  0.62 |    0.32 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 2,759.7 ns | 20,919.83 ns | 1,146.69 ns | 2,423.00 ns |  0.64 |    0.26 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   505.7 ns |  4,745.13 ns |   260.10 ns |   356.00 ns |  0.12 |    0.06 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 4,449.3 ns | 19,107.32 ns | 1,047.34 ns | 4,107.00 ns |  1.03 |    0.29 |     440 B |        1.00 |
