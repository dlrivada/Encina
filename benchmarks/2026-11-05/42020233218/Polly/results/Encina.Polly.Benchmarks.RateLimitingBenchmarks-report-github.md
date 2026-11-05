```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   504.2 ns |     22.47 ns |  21.02 ns |  0.12 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   514.1 ns |     12.54 ns |  11.73 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,723.3 ns |     95.23 ns |  84.42 ns |  0.92 |    0.04 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,597.0 ns |    100.02 ns |  88.67 ns |  0.89 |    0.04 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,341.0 ns |    291.01 ns | 257.97 ns |  0.83 |    0.07 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   456.8 ns |     37.56 ns |  35.14 ns |  0.11 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 4,044.0 ns |    199.02 ns | 176.43 ns |  1.00 |    0.06 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   469.2 ns |    769.78 ns |  42.19 ns |  0.13 |    0.02 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   510.5 ns |    912.19 ns |  50.00 ns |  0.14 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,757.7 ns |  3,490.25 ns | 191.31 ns |  1.04 |    0.15 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,141.0 ns | 10,160.75 ns | 556.95 ns |  1.15 |    0.21 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,413.3 ns |  3,228.69 ns | 176.98 ns |  0.95 |    0.14 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   526.3 ns |    326.52 ns |  17.90 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,663.7 ns | 11,484.05 ns | 629.48 ns |  1.02 |    0.21 |     440 B |        1.00 |
