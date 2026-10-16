```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   167.1 ns |     52.09 ns |    46.18 ns |   174.5 ns |  0.09 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   157.7 ns |     41.00 ns |    36.35 ns |   155.0 ns |  0.08 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,687.7 ns |    894.31 ns |   792.78 ns | 2,330.0 ns |  1.43 |    0.42 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,961.7 ns |    282.99 ns |   236.31 ns | 1,935.5 ns |  1.04 |    0.14 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 1,652.5 ns |    264.93 ns |   221.23 ns | 1,558.0 ns |  0.88 |    0.13 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   162.0 ns |     46.07 ns |    38.47 ns |   157.5 ns |  0.09 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 1,890.5 ns |    168.03 ns |   140.31 ns | 1,891.5 ns |  1.01 |    0.10 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   816.7 ns | 16,854.58 ns |   923.86 ns |   347.0 ns |  0.18 |    0.18 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           | 1,084.8 ns | 17,440.75 ns |   955.99 ns |   903.5 ns |  0.24 |    0.18 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 2,914.5 ns | 28,603.77 ns | 1,567.87 ns | 2,305.5 ns |  0.64 |    0.31 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,220.0 ns | 28,123.74 ns | 1,541.56 ns | 3,910.0 ns |  0.92 |    0.31 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 4,151.0 ns | 21,470.83 ns | 1,176.89 ns | 3,873.0 ns |  0.91 |    0.25 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   312.0 ns |  5,134.47 ns |   281.44 ns |   189.0 ns |  0.07 |    0.05 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 4,635.8 ns | 11,050.07 ns |   605.69 ns | 4,862.5 ns |  1.01 |    0.17 |     440 B |        1.00 |
