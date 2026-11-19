```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   331.0 ns |    104.81 ns |    81.83 ns |   314.5 ns |  0.12 |    0.03 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   206.8 ns |     23.71 ns |    18.51 ns |   205.5 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,112.4 ns |  1,715.92 ns | 1,605.07 ns | 3,811.5 ns |  1.45 |    0.56 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,093.4 ns |    217.91 ns |   181.97 ns | 2,025.0 ns |  0.74 |    0.08 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,248.0 ns |    142.04 ns |   110.90 ns | 2,266.5 ns |  0.79 |    0.07 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   234.7 ns |     87.22 ns |    72.83 ns |   219.0 ns |  0.08 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,852.1 ns |    251.87 ns |   210.32 ns | 2,806.0 ns |  1.00 |    0.10 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   498.2 ns |  6,536.92 ns |   358.31 ns |   362.5 ns |  0.09 |    0.06 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   425.2 ns |  5,642.42 ns |   309.28 ns |   299.5 ns |  0.08 |    0.05 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,100.3 ns | 28,021.08 ns | 1,535.93 ns | 3,440.0 ns |  0.78 |    0.26 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,820.0 ns | 31,875.43 ns | 1,747.20 ns | 3,308.0 ns |  0.72 |    0.29 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 4,190.3 ns | 35,025.74 ns | 1,919.88 ns | 3,471.0 ns |  0.79 |    0.32 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   482.0 ns |  5,954.17 ns |   326.37 ns |   316.0 ns |  0.09 |    0.05 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 5,322.2 ns |  8,946.52 ns |   490.39 ns | 5,521.5 ns |  1.01 |    0.12 |     440 B |        1.00 |
