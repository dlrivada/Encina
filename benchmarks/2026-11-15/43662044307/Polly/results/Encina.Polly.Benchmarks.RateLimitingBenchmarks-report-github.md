```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.52GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                              | Job        | LaunchCount | WarmupCount | Mean       | Error     | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |------------ |------------ |-----------:|----------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | Default     | 5           |   437.4 ns |  97.96 ns |    86.84 ns |  0.18 |    0.04 |         - |        0.00 |
| GetState                            | Job-IAMMPO | Default     | 5           |   271.8 ns |  56.09 ns |    49.72 ns |  0.11 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | Default     | 5           | 4,771.1 ns | 731.34 ns |   648.32 ns |  1.99 |    0.27 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | Default     | 5           | 4,106.3 ns | 632.17 ns |   560.41 ns |  1.71 |    0.24 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | Default     | 5           | 2,635.6 ns | 323.83 ns |   287.07 ns |  1.10 |    0.12 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | Default     | 5           |   309.5 ns |  96.80 ns |    85.81 ns |  0.13 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | Default     | 5           | 2,405.7 ns | 121.67 ns |    94.99 ns |  1.00 |    0.05 |     440 B |        1.00 |
|                                     |            |             |             |            |           |             |       |         |           |             |
| RecordFailure                       | MediumRun  | 2           | 10          |   354.4 ns |  76.58 ns |   109.83 ns |  0.11 |    0.05 |         - |        0.00 |
| GetState                            | MediumRun  | 2           | 10          |   311.9 ns |  76.08 ns |   106.66 ns |  0.10 |    0.05 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 2           | 10          | 4,596.2 ns | 182.36 ns |   249.62 ns |  1.40 |    0.50 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 2           | 10          | 4,587.8 ns | 639.00 ns |   874.68 ns |  1.40 |    0.56 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 2           | 10          | 2,427.5 ns | 240.47 ns |   329.15 ns |  0.74 |    0.28 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 2           | 10          |   344.0 ns |  54.52 ns |    78.19 ns |  0.10 |    0.04 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 2           | 10          | 3,717.6 ns | 917.81 ns | 1,286.64 ns |  1.13 |    0.57 |     440 B |        1.00 |
