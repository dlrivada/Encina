```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.12GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   218.4 ns |     51.58 ns |    45.72 ns |   202.0 ns |  0.06 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   194.4 ns |     26.13 ns |    20.40 ns |   199.5 ns |  0.05 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,730.8 ns |    181.65 ns |   151.68 ns | 1,670.0 ns |  0.48 |    0.10 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,780.8 ns |    251.75 ns |   210.22 ns | 1,666.0 ns |  0.50 |    0.11 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 1,860.4 ns |    114.67 ns |    95.76 ns | 1,834.5 ns |  0.52 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   187.6 ns |     24.39 ns |    21.62 ns |   185.5 ns |  0.05 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,658.0 ns |    563.80 ns |   470.80 ns | 3,710.5 ns |  1.02 |    0.23 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   991.0 ns | 21,898.08 ns | 1,200.31 ns |   344.0 ns |  0.31 |    0.35 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   991.3 ns | 21,018.96 ns | 1,152.12 ns |   385.0 ns |  0.31 |    0.34 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,814.8 ns | 25,219.10 ns | 1,382.34 ns | 4,052.5 ns |  1.49 |    0.64 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 6,109.7 ns | 12,324.39 ns |   675.54 ns | 6,426.0 ns |  1.89 |    0.67 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 5,668.5 ns | 25,130.89 ns | 1,377.51 ns | 5,198.5 ns |  1.75 |    0.71 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   416.2 ns |  5,014.02 ns |   274.84 ns |   260.5 ns |  0.13 |    0.09 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,669.7 ns | 30,641.10 ns | 1,679.54 ns | 3,050.0 ns |  1.13 |    0.61 |     440 B |        1.00 |
