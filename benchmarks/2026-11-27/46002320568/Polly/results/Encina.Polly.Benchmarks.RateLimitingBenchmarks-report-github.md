```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   756.2 ns |    232.45 ns | 217.43 ns |  0.21 |    0.06 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   440.7 ns |     51.12 ns |  42.69 ns |  0.12 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,788.2 ns |    159.91 ns | 133.53 ns |  1.06 |    0.06 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,140.9 ns |    377.11 ns | 352.75 ns |  1.16 |    0.11 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,850.7 ns |    517.56 ns | 484.13 ns |  1.07 |    0.14 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   543.0 ns |     90.75 ns |  84.88 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,592.5 ns |    227.56 ns | 190.03 ns |  1.00 |    0.07 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   657.8 ns |  4,023.57 ns | 220.55 ns |  0.17 |    0.05 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   510.0 ns |  1,377.37 ns |  75.50 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,064.7 ns |  3,855.19 ns | 211.32 ns |  1.06 |    0.07 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,245.0 ns |  7,305.42 ns | 400.43 ns |  1.10 |    0.11 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 4,046.7 ns | 15,521.77 ns | 850.80 ns |  1.05 |    0.20 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   651.7 ns |  3,601.01 ns | 197.38 ns |  0.17 |    0.05 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,857.7 ns |  4,072.90 ns | 223.25 ns |  1.00 |    0.07 |     440 B |        1.00 |
