```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   688.3 ns |    245.72 ns | 229.85 ns |  0.20 |    0.07 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   478.0 ns |     24.43 ns |  21.66 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 8,488.1 ns |  1,088.82 ns | 965.21 ns |  2.45 |    0.30 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,775.0 ns |     58.31 ns |  45.53 ns |  1.09 |    0.06 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,406.0 ns |     94.89 ns |  79.24 ns |  0.98 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   475.5 ns |     51.50 ns |  40.21 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,482.0 ns |    256.96 ns | 214.57 ns |  1.00 |    0.08 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   852.0 ns |  3,036.36 ns | 166.43 ns |  0.24 |    0.04 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   696.5 ns |  5,465.84 ns | 299.60 ns |  0.20 |    0.07 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,856.7 ns |  2,506.13 ns | 137.37 ns |  1.09 |    0.07 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,902.7 ns |  3,486.43 ns | 191.10 ns |  1.11 |    0.08 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,898.8 ns | 12,949.72 ns | 709.82 ns |  1.11 |    0.18 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   474.7 ns |  1,409.65 ns |  77.27 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,535.7 ns |  4,035.55 ns | 221.20 ns |  1.00 |    0.08 |     440 B |        1.00 |
