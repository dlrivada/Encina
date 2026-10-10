```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   292.4 ns |     78.08 ns |    69.22 ns |   274.5 ns |  0.10 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   214.4 ns |     40.25 ns |    35.68 ns |   210.0 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,490.7 ns |  1,379.27 ns | 1,151.75 ns | 3,513.0 ns |  1.22 |    0.40 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,274.4 ns |    548.53 ns |   486.26 ns | 2,055.5 ns |  0.80 |    0.18 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,737.8 ns |    252.03 ns |   210.46 ns | 2,687.0 ns |  0.96 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   195.6 ns |     24.63 ns |    21.84 ns |   196.0 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,876.9 ns |    259.70 ns |   230.22 ns | 2,824.0 ns |  1.01 |    0.11 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           | 1,162.2 ns | 12,393.51 ns |   679.33 ns | 1,019.5 ns |  0.39 |    0.26 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   400.5 ns |  5,004.94 ns |   274.34 ns |   335.5 ns |  0.13 |    0.10 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 5,713.7 ns | 36,513.78 ns | 2,001.44 ns | 4,836.0 ns |  1.92 |    0.95 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,838.7 ns | 24,856.53 ns | 1,362.47 ns | 3,440.0 ns |  1.29 |    0.64 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 5,780.3 ns | 26,693.69 ns | 1,463.17 ns | 5,267.0 ns |  1.94 |    0.86 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           | 1,167.8 ns | 27,813.21 ns | 1,524.54 ns |   388.5 ns |  0.39 |    0.49 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,466.8 ns | 31,824.51 ns | 1,744.41 ns | 2,860.5 ns |  1.17 |    0.69 |     440 B |        1.00 |
