```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                              | Job        | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | Default     | 5           |   510.9 ns |  18.82 ns |  17.60 ns |  0.15 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | Default     | 5           |   545.7 ns |  52.07 ns |  48.71 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | Default     | 5           | 3,717.1 ns |  70.69 ns |  62.66 ns |  1.07 |    0.04 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | Default     | 5           | 3,911.1 ns | 143.72 ns | 127.41 ns |  1.12 |    0.05 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | Default     | 5           | 4,141.9 ns | 192.59 ns | 170.73 ns |  1.19 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | Default     | 5           |   499.3 ns |  28.20 ns |  25.00 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | Default     | 5           | 3,484.3 ns | 134.59 ns | 119.31 ns |  1.00 |    0.05 |     440 B |        1.00 |
|                                     |            |             |             |            |           |           |       |         |           |             |
| RecordFailure                       | MediumRun  | 2           | 10          |   566.4 ns |  23.40 ns |  32.80 ns |  0.16 |    0.01 |         - |        0.00 |
| GetState                            | MediumRun  | 2           | 10          |   532.5 ns |  24.73 ns |  36.25 ns |  0.15 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 2           | 10          | 3,769.3 ns | 106.77 ns | 149.67 ns |  1.07 |    0.05 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 2           | 10          | 3,781.1 ns |  58.86 ns |  80.56 ns |  1.07 |    0.04 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 2           | 10          | 3,468.0 ns |  68.73 ns |  94.08 ns |  0.99 |    0.04 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 2           | 10          |   559.0 ns |  16.24 ns |  24.31 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 2           | 10          | 3,522.9 ns |  89.23 ns | 122.14 ns |  1.00 |    0.05 |     440 B |        1.00 |
