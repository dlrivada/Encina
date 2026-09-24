```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   315.11 ns |    562.63 ns |   498.76 ns |    75.00 ns |  0.14 |    0.21 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   111.64 ns |     97.79 ns |    86.68 ns |    90.00 ns |  0.05 |    0.04 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           |   820.81 ns |    184.93 ns |   154.42 ns |   790.50 ns |  0.36 |    0.07 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,723.29 ns |    548.77 ns |   486.47 ns | 1,592.00 ns |  0.76 |    0.22 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |   970.71 ns |    252.95 ns |   224.24 ns | 1,001.00 ns |  0.43 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    39.62 ns |     31.16 ns |    26.02 ns |    35.00 ns |  0.02 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,272.14 ns |    196.43 ns |   174.13 ns | 2,229.00 ns |  1.01 |    0.10 |     440 B |        1.00 |
|                                     |            |                |             |             |             |              |             |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   242.33 ns |  3,451.89 ns |   189.21 ns |   175.00 ns |  0.09 |    0.06 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   226.50 ns |  4,219.67 ns |   231.29 ns |   129.50 ns |  0.08 |    0.07 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 2,968.33 ns |  6,404.39 ns |   351.05 ns | 3,135.00 ns |  1.09 |    0.16 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 2,140.00 ns | 21,179.69 ns | 1,160.93 ns | 1,753.00 ns |  0.79 |    0.38 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,031.33 ns | 16,790.19 ns |   920.33 ns | 2,724.00 ns |  1.12 |    0.32 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   193.67 ns |  4,382.68 ns |   240.23 ns |    60.00 ns |  0.07 |    0.08 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,741.00 ns |  5,916.37 ns |   324.30 ns | 2,865.00 ns |  1.01 |    0.15 |     440 B |        1.00 |
