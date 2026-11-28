```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.43GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |    82.58 ns |     42.78 ns |    33.40 ns |   100.00 ns |  0.10 |    0.05 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |    33.33 ns |     13.74 ns |    10.73 ns |    30.00 ns |  0.04 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           |   808.46 ns |    168.85 ns |   141.00 ns |   786.00 ns |  1.01 |    0.28 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,000.21 ns |    315.76 ns |   279.91 ns |   882.00 ns |  1.25 |    0.44 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 1,594.60 ns |    746.75 ns |   698.51 ns | 1,162.00 ns |  1.99 |    0.97 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    21.62 ns |     35.84 ns |    29.93 ns |    11.00 ns |  0.03 |    0.04 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           |   839.31 ns |    221.54 ns |   185.00 ns |   836.00 ns |  1.05 |    0.32 |     440 B |        1.00 |
|                                     |            |                |             |             |             |              |             |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   177.00 ns |  3,386.02 ns |   185.60 ns |    80.00 ns |  0.10 |    0.10 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   187.33 ns |  3,547.47 ns |   194.45 ns |   101.00 ns |  0.11 |    0.11 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 2,171.33 ns | 29,940.34 ns | 1,641.13 ns | 1,557.00 ns |  1.24 |    0.93 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 1,905.50 ns | 22,503.77 ns | 1,233.51 ns | 1,381.50 ns |  1.08 |    0.72 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 1,894.67 ns | 16,674.35 ns |   913.98 ns | 1,408.00 ns |  1.08 |    0.58 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           | 1,363.67 ns |  3,854.28 ns |   211.27 ns | 1,257.00 ns |  0.78 |    0.27 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 1,968.33 ns | 16,058.02 ns |   880.19 ns | 1,547.00 ns |  1.12 |    0.57 |     440 B |        1.00 |
