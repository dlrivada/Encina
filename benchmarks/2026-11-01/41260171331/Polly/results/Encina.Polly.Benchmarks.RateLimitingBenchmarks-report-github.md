```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                              | Job        | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | Default     | 5           |   510.1 ns |  28.97 ns |  27.10 ns |  0.15 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | Default     | 5           |   446.5 ns |  23.70 ns |  19.79 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | Default     | 5           | 3,590.2 ns | 270.13 ns | 225.57 ns |  1.08 |    0.07 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | Default     | 5           | 3,870.5 ns | 344.87 ns | 322.59 ns |  1.17 |    0.10 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | Default     | 5           | 3,648.7 ns | 338.21 ns | 316.37 ns |  1.10 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | Default     | 5           |   525.2 ns |  28.55 ns |  22.29 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | Default     | 5           | 3,319.1 ns |  93.61 ns |  78.16 ns |  1.00 |    0.03 |     440 B |        1.00 |
|                                     |            |             |             |            |           |           |       |         |           |             |
| RecordFailure                       | MediumRun  | 2           | 10          |   413.9 ns |  48.31 ns |  66.13 ns |  0.12 |    0.02 |         - |        0.00 |
| GetState                            | MediumRun  | 2           | 10          |   508.4 ns |  46.26 ns |  69.24 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 2           | 10          | 3,877.7 ns | 208.09 ns | 298.43 ns |  1.14 |    0.09 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 2           | 10          | 3,743.5 ns |  84.65 ns | 115.87 ns |  1.10 |    0.05 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 2           | 10          | 3,343.1 ns |  90.29 ns | 129.49 ns |  0.99 |    0.05 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 2           | 10          |   458.7 ns |  24.94 ns |  34.14 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 2           | 10          | 3,394.9 ns |  63.16 ns |  94.53 ns |  1.00 |    0.04 |     440 B |        1.00 |
