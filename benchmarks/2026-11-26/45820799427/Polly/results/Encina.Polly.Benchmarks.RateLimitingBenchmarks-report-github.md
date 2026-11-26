```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |    50.92 ns |     12.87 ns |    10.05 ns |    50.00 ns |  0.05 |    0.04 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |    54.15 ns |     48.42 ns |    40.43 ns |    40.00 ns |  0.06 |    0.06 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,446.60 ns |    848.95 ns |   794.11 ns | 1,116.00 ns |  1.48 |    1.33 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           |   899.08 ns |    213.58 ns |   178.35 ns |   851.00 ns |  0.92 |    0.63 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |   736.29 ns |    209.57 ns |   185.78 ns |   661.00 ns |  0.75 |    0.53 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    55.83 ns |     20.77 ns |    16.21 ns |    55.00 ns |  0.06 |    0.04 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 1,605.37 ns |  1,192.56 ns | 1,115.52 ns | 1,191.50 ns |  1.64 |    1.69 |     440 B |        1.00 |
|                                     |            |                |             |             |             |              |             |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   222.00 ns |  3,365.31 ns |   184.46 ns |   116.00 ns |  0.07 |    0.05 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           | 1,032.83 ns | 15,842.47 ns |   868.38 ns | 1,256.50 ns |  0.32 |    0.25 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 1,904.33 ns | 20,052.02 ns | 1,099.12 ns | 1,678.00 ns |  0.58 |    0.34 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 1,978.33 ns | 23,068.48 ns | 1,264.46 ns | 1,388.00 ns |  0.60 |    0.39 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 1,965.00 ns | 17,978.53 ns |   985.46 ns | 1,468.00 ns |  0.60 |    0.32 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   754.67 ns | 11,528.42 ns |   631.91 ns |   781.00 ns |  0.23 |    0.18 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,565.67 ns | 24,927.59 ns | 1,366.37 ns | 2,975.00 ns |  1.09 |    0.48 |     440 B |        1.00 |
