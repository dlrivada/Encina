```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error         | StdDev       | Median        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-------------:|--------------:|-------------:|--------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   520.867 ns |    728.889 ns |   681.803 ns |    41.0000 ns |  0.84 |    1.11 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |     7.846 ns |     17.768 ns |    14.837 ns |     0.0000 ns |  0.01 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,052.500 ns |    310.167 ns |   274.955 ns |   916.0000 ns |  1.71 |    0.58 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,304.154 ns |    490.272 ns |   409.399 ns | 2,394.0000 ns |  3.74 |    1.06 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |   766.917 ns |    104.415 ns |    81.520 ns |   756.0000 ns |  1.24 |    0.31 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    98.462 ns |     67.875 ns |    56.678 ns |    85.0000 ns |  0.16 |    0.10 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           |   653.583 ns |    225.956 ns |   176.411 ns |   621.0000 ns |  1.06 |    0.37 |     440 B |        1.00 |
|                                     |            |                |             |             |              |               |              |               |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           | 1,172.000 ns | 10,522.390 ns |   576.768 ns | 1,432.0000 ns |  0.73 |    0.46 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           | 1,179.833 ns | 10,679.738 ns |   585.392 ns | 1,476.5000 ns |  0.74 |    0.47 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 2,153.333 ns | 22,403.502 ns | 1,228.012 ns | 1,693.0000 ns |  1.34 |    0.93 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 1,907.833 ns | 21,939.185 ns | 1,202.561 ns | 1,507.5000 ns |  1.19 |    0.87 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,202.000 ns | 18,861.654 ns | 1,033.871 ns | 2,955.0000 ns |  2.00 |    1.06 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           | 1,005.000 ns | 14,304.859 ns |   784.098 ns | 1,282.0000 ns |  0.63 |    0.53 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 1,948.000 ns | 19,465.667 ns | 1,066.979 ns | 1,658.0000 ns |  1.21 |    0.82 |     440 B |        1.00 |
