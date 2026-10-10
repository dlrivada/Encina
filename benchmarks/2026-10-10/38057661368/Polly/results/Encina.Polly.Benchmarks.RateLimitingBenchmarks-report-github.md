```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |    47.00 ns |     29.47 ns |    23.01 ns |    44.50 ns |  0.08 |    0.04 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |    69.64 ns |     30.88 ns |    27.37 ns |    69.50 ns |  0.12 |    0.05 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,011.43 ns |    369.45 ns |   327.51 ns |   891.00 ns |  1.74 |    0.58 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           |   978.79 ns |    284.11 ns |   251.86 ns |   901.50 ns |  1.68 |    0.46 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |   847.07 ns |    243.45 ns |   215.81 ns |   811.00 ns |  1.46 |    0.39 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   113.38 ns |     48.71 ns |    40.68 ns |   110.00 ns |  0.19 |    0.07 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           |   589.38 ns |     83.05 ns |    69.35 ns |   581.00 ns |  1.01 |    0.16 |     440 B |        1.00 |
|                                     |            |                |             |             |             |              |             |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   202.33 ns |  2,915.59 ns |   159.81 ns |   126.00 ns |  0.11 |    0.09 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   230.33 ns |  3,630.01 ns |   198.97 ns |   121.00 ns |  0.12 |    0.11 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 2,009.67 ns | 20,103.04 ns | 1,101.92 ns | 1,613.00 ns |  1.06 |    0.74 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,321.33 ns | 19,886.11 ns | 1,090.02 ns | 3,064.00 ns |  1.75 |    0.98 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 2,006.33 ns | 17,840.80 ns |   977.91 ns | 1,472.00 ns |  1.06 |    0.69 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   247.00 ns |  2,173.76 ns |   119.15 ns |   211.00 ns |  0.13 |    0.09 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,243.67 ns | 17,468.42 ns |   957.50 ns | 2,544.00 ns |  1.18 |    0.73 |     440 B |        1.00 |
