```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   217.2 ns |    171.38 ns | 151.92 ns |   170.0 ns |  0.09 |    0.06 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   172.7 ns |     91.57 ns |  81.17 ns |   171.0 ns |  0.07 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,865.2 ns |    228.06 ns | 213.33 ns | 2,844.0 ns |  1.23 |    0.12 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,599.0 ns |     68.77 ns |  57.43 ns | 2,583.5 ns |  1.11 |    0.08 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,439.1 ns |    212.06 ns | 187.99 ns | 2,408.5 ns |  1.05 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   307.5 ns |     20.89 ns |  18.51 ns |   310.5 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,342.5 ns |    187.48 ns | 166.19 ns | 2,317.5 ns |  1.00 |    0.10 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   507.7 ns |  3,303.79 ns | 181.09 ns |   501.0 ns |  0.19 |    0.06 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   453.5 ns |  1,701.37 ns |  93.26 ns |   410.5 ns |  0.17 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,175.0 ns |  2,219.44 ns | 121.66 ns | 3,235.0 ns |  1.19 |    0.12 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,221.7 ns | 11,922.58 ns | 653.52 ns | 3,005.0 ns |  1.21 |    0.24 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,170.3 ns |  3,434.98 ns | 188.28 ns | 3,274.0 ns |  1.19 |    0.13 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   441.0 ns |  2,241.83 ns | 122.88 ns |   391.0 ns |  0.17 |    0.04 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,687.7 ns |  5,304.20 ns | 290.74 ns | 2,674.0 ns |  1.01 |    0.13 |     440 B |        1.00 |
