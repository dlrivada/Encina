```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   295.2 ns |     33.93 ns |    26.49 ns |   294.5 ns |  0.06 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   260.6 ns |     77.29 ns |    68.51 ns |   250.5 ns |  0.06 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,129.6 ns |    276.77 ns |   231.12 ns | 4,105.0 ns |  0.88 |    0.07 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,431.5 ns |    624.25 ns |   521.28 ns | 2,274.0 ns |  0.52 |    0.11 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,337.2 ns |    279.18 ns |   233.13 ns | 2,260.5 ns |  0.50 |    0.05 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   183.1 ns |     51.72 ns |    45.85 ns |   176.0 ns |  0.04 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 4,718.1 ns |    305.28 ns |   270.62 ns | 4,654.0 ns |  1.00 |    0.08 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   436.5 ns |  6,777.41 ns |   371.49 ns |   258.5 ns |  0.14 |    0.12 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           | 1,063.2 ns | 24,101.31 ns | 1,321.07 ns |   316.5 ns |  0.34 |    0.40 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,396.0 ns | 29,865.24 ns | 1,637.01 ns | 2,687.0 ns |  1.08 |    0.62 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,413.7 ns | 30,732.86 ns | 1,684.57 ns | 2,691.0 ns |  1.09 |    0.63 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,571.2 ns |  5,811.54 ns |   318.55 ns | 3,745.5 ns |  1.14 |    0.42 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   476.7 ns |  6,419.93 ns |   351.90 ns |   275.0 ns |  0.15 |    0.12 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,588.3 ns | 30,944.91 ns | 1,696.19 ns | 3,023.0 ns |  1.15 |    0.64 |     440 B |        1.00 |
