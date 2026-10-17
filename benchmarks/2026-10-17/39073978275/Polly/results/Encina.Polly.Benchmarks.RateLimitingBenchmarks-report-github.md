```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   617.2 ns |   154.52 ns | 144.54 ns |   541.0 ns |  0.16 |    0.04 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   480.9 ns |    15.81 ns |  13.20 ns |   480.0 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,776.0 ns |    48.82 ns |  43.28 ns | 3,777.5 ns |  1.01 |    0.11 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,677.2 ns |    80.83 ns |  67.49 ns | 3,666.0 ns |  0.98 |    0.11 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,399.1 ns |    70.88 ns |  62.83 ns | 3,416.0 ns |  0.91 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   538.7 ns |    16.24 ns |  15.19 ns |   542.0 ns |  0.14 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,794.8 ns |   506.08 ns | 422.60 ns | 4,107.0 ns |  1.01 |    0.16 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   534.2 ns |   732.10 ns |  40.13 ns |   511.5 ns |  0.14 |    0.01 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   388.3 ns | 1,526.42 ns |  83.67 ns |   362.0 ns |  0.11 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,034.7 ns | 1,916.31 ns | 105.04 ns | 4,038.0 ns |  1.09 |    0.04 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,927.7 ns | 4,504.40 ns | 246.90 ns | 3,958.0 ns |  1.06 |    0.06 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,893.8 ns | 7,543.32 ns | 413.47 ns | 3,756.5 ns |  1.06 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   679.8 ns | 5,174.06 ns | 283.61 ns |   536.5 ns |  0.18 |    0.07 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,693.2 ns | 2,137.97 ns | 117.19 ns | 3,646.5 ns |  1.00 |    0.04 |     440 B |        1.00 |
