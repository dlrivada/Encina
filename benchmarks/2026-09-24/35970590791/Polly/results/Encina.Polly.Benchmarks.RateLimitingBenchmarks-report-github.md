```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   498.1 ns |     53.90 ns |  47.78 ns |  0.15 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   522.0 ns |     31.58 ns |  28.00 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,853.4 ns |    139.68 ns | 123.82 ns |  1.18 |    0.04 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,738.9 ns |     43.23 ns |  36.10 ns |  1.14 |    0.02 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,593.9 ns |    224.19 ns | 198.73 ns |  1.10 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   593.8 ns |     82.27 ns |  72.93 ns |  0.18 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,274.8 ns |     49.75 ns |  41.54 ns |  1.00 |    0.02 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   546.2 ns |  1,115.75 ns |  61.16 ns |  0.13 |    0.02 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   561.7 ns |    305.46 ns |  16.74 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,961.3 ns |  5,275.98 ns | 289.19 ns |  0.95 |    0.15 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,876.7 ns |  2,221.72 ns | 121.78 ns |  0.93 |    0.14 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,911.0 ns |  5,050.15 ns | 276.82 ns |  0.94 |    0.15 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   775.3 ns |  2,281.07 ns | 125.03 ns |  0.19 |    0.04 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 4,244.0 ns | 11,819.45 ns | 647.86 ns |  1.02 |    0.20 |     440 B |        1.00 |
