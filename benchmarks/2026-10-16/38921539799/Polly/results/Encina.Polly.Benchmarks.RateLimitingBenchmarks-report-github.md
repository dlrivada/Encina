```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   262.4 ns |     63.41 ns |    56.22 ns |   264.0 ns |  0.16 |    0.04 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   200.9 ns |    250.91 ns |   195.90 ns |   122.0 ns |  0.12 |    0.12 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,409.3 ns |  1,479.19 ns | 1,383.63 ns | 1,630.5 ns |  1.47 |    0.83 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,634.4 ns |    987.19 ns |   923.42 ns | 3,863.5 ns |  2.21 |    0.59 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,254.0 ns |    307.81 ns |   257.04 ns | 2,194.0 ns |  1.37 |    0.21 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           | 1,032.9 ns |    855.90 ns |   800.61 ns | 1,428.0 ns |  0.63 |    0.48 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 1,661.8 ns |    233.06 ns |   194.61 ns | 1,558.0 ns |  1.01 |    0.15 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   464.0 ns |  3,182.94 ns |   174.47 ns |   427.0 ns |  0.15 |    0.09 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   389.8 ns |  7,014.93 ns |   384.51 ns |   216.5 ns |  0.13 |    0.13 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,322.8 ns | 40,012.23 ns | 2,193.21 ns | 3,573.5 ns |  1.39 |    0.97 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,252.7 ns | 42,176.73 ns | 2,311.85 ns | 3,555.0 ns |  1.37 |    0.99 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 5,203.5 ns | 20,338.85 ns | 1,114.84 ns | 4,772.5 ns |  1.68 |    0.91 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   434.3 ns |  4,736.22 ns |   259.61 ns |   360.0 ns |  0.14 |    0.11 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,955.3 ns | 42,779.12 ns | 2,344.87 ns | 3,437.0 ns |  1.28 |    0.97 |     440 B |        1.00 |
