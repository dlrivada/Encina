```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                              | Job        | LaunchCount | WarmupCount | Mean        | Error      | StdDev     | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |------------ |------------ |------------:|-----------:|-----------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | Default     | 5           |    82.92 ns |  30.655 ns |  23.933 ns |    79.50 ns |  0.10 |    0.03 |         - |        0.00 |
| GetState                            | Job-IAMMPO | Default     | 5           |    50.08 ns |   0.370 ns |   0.289 ns |    50.00 ns |  0.06 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | Default     | 5           | 1,068.69 ns | 328.412 ns | 274.239 ns | 1,032.00 ns |  1.28 |    0.37 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | Default     | 5           | 2,610.71 ns | 277.512 ns | 246.007 ns | 2,588.50 ns |  3.12 |    0.54 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | Default     | 5           |   773.92 ns | 186.437 ns | 155.683 ns |   706.00 ns |  0.92 |    0.23 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | Default     | 5           |    66.50 ns |  41.443 ns |  32.356 ns |    69.50 ns |  0.08 |    0.04 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | Default     | 5           |   854.75 ns | 165.186 ns | 128.967 ns |   876.50 ns |  1.02 |    0.21 |     440 B |        1.00 |
|                                     |            |             |             |             |            |            |             |       |         |           |             |
| RecordFailure                       | MediumRun  | 2           | 10          |   460.18 ns | 342.600 ns | 512.788 ns |   265.50 ns |  0.20 |    0.22 |         - |        0.00 |
| GetState                            | MediumRun  | 2           | 10          |    45.31 ns |  19.154 ns |  26.219 ns |    38.00 ns |  0.02 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 2           | 10          |   718.88 ns |  88.632 ns | 121.321 ns |   681.00 ns |  0.31 |    0.05 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 2           | 10          |   866.66 ns | 184.171 ns | 245.863 ns |   841.50 ns |  0.37 |    0.11 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 2           | 10          |   827.12 ns |  86.441 ns | 115.397 ns |   835.50 ns |  0.35 |    0.05 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 2           | 10          |   459.64 ns | 433.784 ns | 622.120 ns |   120.00 ns |  0.20 |    0.26 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 2           | 10          | 2,337.76 ns |  90.144 ns | 120.339 ns | 2,303.00 ns |  1.00 |    0.07 |     440 B |        1.00 |
