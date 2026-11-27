```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   334.3 ns |    34.63 ns |  30.70 ns |  0.13 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   235.3 ns |    30.94 ns |  25.84 ns |  0.09 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,526.2 ns |   111.05 ns |  92.73 ns |  0.97 |    0.06 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,854.0 ns |   225.75 ns | 200.12 ns |  1.10 |    0.10 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,535.7 ns |    91.20 ns |  71.20 ns |  0.98 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   348.7 ns |    46.19 ns |  40.95 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,600.1 ns |   158.51 ns | 148.27 ns |  1.00 |    0.08 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   409.7 ns | 2,112.68 ns | 115.80 ns |  0.15 |    0.04 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   448.8 ns | 1,551.61 ns |  85.05 ns |  0.17 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,449.0 ns | 2,809.00 ns | 153.97 ns |  1.28 |    0.11 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 2,812.2 ns | 6,580.41 ns | 360.69 ns |  1.04 |    0.14 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 2,640.5 ns | 6,262.06 ns | 343.24 ns |  0.98 |    0.14 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   521.0 ns | 1,590.45 ns |  87.18 ns |  0.19 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,712.7 ns | 4,666.27 ns | 255.77 ns |  1.01 |    0.11 |     440 B |        1.00 |
