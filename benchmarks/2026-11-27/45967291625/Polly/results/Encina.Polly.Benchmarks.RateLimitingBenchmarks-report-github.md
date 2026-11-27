```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev       | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |    30.85 ns |     11.42 ns |     9.538 ns |    30.00 ns |  0.04 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |    53.79 ns |     13.79 ns |    12.223 ns |    50.00 ns |  0.06 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,394.77 ns |     65.89 ns |    55.024 ns | 2,389.00 ns |  2.83 |    0.52 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,107.00 ns |    336.63 ns |   298.416 ns | 1,111.50 ns |  1.31 |    0.42 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 1,582.86 ns |  1,009.67 ns |   895.047 ns | 1,131.50 ns |  1.87 |    1.09 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    51.71 ns |     33.60 ns |    29.784 ns |    54.50 ns |  0.06 |    0.04 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           |   883.08 ns |    269.91 ns |   225.383 ns |   791.00 ns |  1.05 |    0.32 |     440 B |        1.00 |
|                                     |            |                |             |             |             |              |              |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   302.00 ns |  2,773.23 ns |   152.010 ns |   235.00 ns |  0.19 |    0.12 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           | 1,095.00 ns | 17,546.90 ns |   961.805 ns | 1,322.00 ns |  0.70 |    0.63 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 2,197.83 ns | 21,536.89 ns | 1,180.509 ns | 1,897.50 ns |  1.41 |    0.88 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 2,860.50 ns |  4,424.13 ns |   242.502 ns | 2,963.50 ns |  1.83 |    0.72 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 2,356.67 ns | 11,415.82 ns |   625.740 ns | 2,073.00 ns |  1.51 |    0.69 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   304.33 ns |  6,955.72 ns |   381.267 ns |   181.00 ns |  0.20 |    0.24 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 1,866.00 ns | 19,104.24 ns | 1,047.168 ns | 1,402.00 ns |  1.20 |    0.77 |     440 B |        1.00 |
