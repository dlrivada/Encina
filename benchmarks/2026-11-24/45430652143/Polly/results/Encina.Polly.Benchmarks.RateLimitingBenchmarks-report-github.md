```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |    30.77 ns |     31.29 ns |    26.13 ns |    25.00 ns |  0.01 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |    30.85 ns |     49.17 ns |    41.06 ns |    10.00 ns |  0.01 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           |   910.93 ns |    294.15 ns |   260.76 ns |   771.50 ns |  0.43 |    0.14 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,319.29 ns |    793.02 ns |   702.99 ns | 1,006.00 ns |  0.62 |    0.34 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |   728.67 ns |    150.23 ns |   117.29 ns |   681.00 ns |  0.34 |    0.08 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    60.15 ns |     35.46 ns |    29.61 ns |    51.00 ns |  0.03 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,206.36 ns |    496.08 ns |   439.76 ns | 2,143.00 ns |  1.03 |    0.27 |     440 B |        1.00 |
|                                     |            |                |             |             |             |              |             |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   691.00 ns | 11,265.86 ns |   617.52 ns |   641.00 ns |  0.52 |    0.61 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           | 1,048.33 ns | 15,819.24 ns |   867.11 ns | 1,312.00 ns |  0.79 |    0.88 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,420.67 ns | 16,081.84 ns |   881.50 ns | 3,421.00 ns |  2.59 |    1.93 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 1,993.00 ns | 24,760.87 ns | 1,357.23 ns | 1,513.00 ns |  1.51 |    1.50 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,384.83 ns | 11,669.14 ns |   639.63 ns | 3,164.50 ns |  2.57 |    1.86 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   284.00 ns |  4,050.64 ns |   222.03 ns |   220.00 ns |  0.22 |    0.23 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,213.00 ns | 34,272.07 ns | 1,878.57 ns | 1,622.00 ns |  1.68 |    1.88 |     440 B |        1.00 |
