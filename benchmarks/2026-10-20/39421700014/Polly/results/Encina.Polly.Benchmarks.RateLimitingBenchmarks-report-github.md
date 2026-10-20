```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |    40.54 ns |     29.42 ns |    24.57 ns |    26.00 ns |  0.06 |    0.03 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |    30.31 ns |     49.38 ns |    41.23 ns |    10.00 ns |  0.04 |    0.06 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,034.57 ns |    330.97 ns |   293.40 ns |   967.00 ns |  1.43 |    0.46 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,902.27 ns |    376.80 ns |   314.65 ns | 1,992.50 ns |  2.63 |    0.59 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |   533.15 ns |    160.47 ns |   134.00 ns |   521.00 ns |  0.74 |    0.21 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    73.57 ns |     35.56 ns |    31.53 ns |    80.00 ns |  0.10 |    0.05 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           |   742.69 ns |    158.25 ns |   132.14 ns |   711.00 ns |  1.03 |    0.24 |     440 B |        1.00 |
|                                     |            |                |             |             |             |              |             |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   574.33 ns | 16,977.22 ns |   930.58 ns |    75.00 ns |  0.43 |    0.67 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   256.67 ns |  4,279.83 ns |   234.59 ns |   180.00 ns |  0.19 |    0.18 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 1,766.00 ns | 22,416.34 ns | 1,228.72 ns | 1,322.00 ns |  1.32 |    1.01 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,046.00 ns | 18,234.77 ns |   999.51 ns | 2,599.00 ns |  2.28 |    1.16 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 1,719.33 ns | 24,564.64 ns | 1,346.47 ns | 1,092.00 ns |  1.29 |    1.07 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   213.67 ns |  4,058.17 ns |   222.44 ns |   150.00 ns |  0.16 |    0.17 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 1,656.33 ns | 18,852.01 ns | 1,033.34 ns | 1,152.00 ns |  1.24 |    0.88 |     440 B |        1.00 |
