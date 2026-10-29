```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   413.2 ns |    51.83 ns |  48.48 ns |   406.0 ns |  0.15 |    0.03 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   332.4 ns |    52.12 ns |  46.21 ns |   320.5 ns |  0.12 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,741.5 ns |   314.02 ns | 293.74 ns | 2,664.0 ns |  1.00 |    0.16 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,782.5 ns |   195.05 ns | 172.90 ns | 2,784.0 ns |  1.02 |    0.14 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,618.1 ns |   268.93 ns | 251.56 ns | 2,554.0 ns |  0.96 |    0.15 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   345.0 ns |    57.50 ns |  50.98 ns |   330.5 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,772.0 ns |   430.02 ns | 381.20 ns | 2,669.5 ns |  1.02 |    0.18 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   347.3 ns | 1,646.02 ns |  90.22 ns |   340.0 ns |  0.13 |    0.03 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   371.0 ns |   631.98 ns |  34.64 ns |   391.0 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,121.7 ns | 3,325.83 ns | 182.30 ns | 3,155.0 ns |  1.13 |    0.13 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 2,750.0 ns | 8,995.25 ns | 493.06 ns | 2,654.0 ns |  0.99 |    0.19 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 2,744.7 ns | 5,095.53 ns | 279.30 ns | 2,825.0 ns |  0.99 |    0.14 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   604.3 ns | 5,010.66 ns | 274.65 ns |   461.0 ns |  0.22 |    0.09 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,795.5 ns | 5,806.18 ns | 318.26 ns | 2,898.5 ns |  1.01 |    0.14 |     440 B |        1.00 |
