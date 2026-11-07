```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|--------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   352.60 ns |    504.79 ns |   472.18 ns |    90.0000 ns |  0.61 |    0.83 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   421.67 ns |    590.93 ns |   552.76 ns |     0.0000 ns |  0.73 |    0.97 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           |   817.33 ns |    163.51 ns |   127.66 ns |   796.5000 ns |  1.42 |    0.38 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           |   740.50 ns |    198.25 ns |   154.78 ns |   716.5000 ns |  1.29 |    0.39 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |   929.36 ns |    775.94 ns |   687.85 ns |   631.5000 ns |  1.62 |    1.24 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    26.00 ns |     12.39 ns |    10.99 ns |    24.5000 ns |  0.05 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           |   601.25 ns |    169.81 ns |   132.57 ns |   596.0000 ns |  1.05 |    0.32 |     440 B |        1.00 |
|                                     |            |                |             |             |             |              |             |               |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   235.67 ns |  3,980.32 ns |   218.18 ns |   135.0000 ns |  0.22 |    0.24 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   174.00 ns |  5,016.43 ns |   274.97 ns |    31.0000 ns |  0.16 |    0.27 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,366.17 ns | 16,582.98 ns |   908.97 ns | 3,189.5000 ns |  3.09 |    2.00 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 1,856.17 ns | 25,252.41 ns | 1,384.17 ns | 1,421.5000 ns |  1.70 |    1.61 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 1,893.00 ns | 22,855.41 ns | 1,252.78 ns | 1,412.0000 ns |  1.74 |    1.53 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           | 1,110.00 ns |  5,645.75 ns |   309.46 ns | 1,167.0000 ns |  1.02 |    0.66 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 1,691.00 ns | 26,078.70 ns | 1,429.46 ns | 1,107.0000 ns |  1.55 |    1.59 |     440 B |        1.00 |
