```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   379.3 ns |   126.83 ns | 118.64 ns |  0.15 |    0.05 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   451.8 ns |   156.46 ns | 146.35 ns |  0.17 |    0.06 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,706.8 ns |   205.43 ns | 182.11 ns |  1.04 |    0.10 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,956.9 ns |   366.70 ns | 325.07 ns |  1.14 |    0.14 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,472.2 ns |   158.44 ns | 140.45 ns |  0.95 |    0.08 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   372.7 ns |    47.31 ns |  41.94 ns |  0.14 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,608.4 ns |   194.12 ns | 181.58 ns |  1.00 |    0.10 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   314.0 ns |   742.63 ns |  40.71 ns |  0.12 |    0.02 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   391.0 ns | 1,930.73 ns | 105.83 ns |  0.15 |    0.04 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,075.3 ns | 7,595.84 ns | 416.35 ns |  1.15 |    0.16 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,242.0 ns | 8,219.54 ns | 450.54 ns |  1.22 |    0.17 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 2,780.8 ns | 6,673.07 ns | 365.77 ns |  1.04 |    0.14 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   379.0 ns | 1,000.41 ns |  54.84 ns |  0.14 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,677.7 ns | 3,909.88 ns | 214.31 ns |  1.00 |    0.10 |     440 B |        1.00 |
