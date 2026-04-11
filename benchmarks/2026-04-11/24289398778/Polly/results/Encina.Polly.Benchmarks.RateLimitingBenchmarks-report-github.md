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
| RecordFailure                       | Job-IAMMPO | Default     | 5           |   528.2 ns |  92.67 ns |  86.69 ns |  0.14 |    0.03 |         - |        0.00 |
| GetState                            | Job-IAMMPO | Default     | 5           |   556.5 ns |  93.76 ns |  87.70 ns |  0.15 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | Default     | 5           | 4,090.7 ns | 602.27 ns | 563.36 ns |  1.11 |    0.17 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | Default     | 5           | 3,706.1 ns | 280.33 ns | 234.09 ns |  1.00 |    0.10 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | Default     | 5           | 3,550.3 ns | 287.10 ns | 268.55 ns |  0.96 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | Default     | 5           |   557.8 ns |  87.17 ns |  77.27 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | Default     | 5           | 3,717.4 ns | 381.30 ns | 318.40 ns |  1.01 |    0.11 |     440 B |        1.00 |
|                                     |            |             |             |            |           |           |       |         |           |             |
| RecordFailure                       | MediumRun  | 2           | 10          |   582.6 ns |  60.86 ns |  91.10 ns |  0.17 |    0.03 |         - |        0.00 |
| GetState                            | MediumRun  | 2           | 10          |   558.1 ns |  58.75 ns |  82.36 ns |  0.16 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 2           | 10          | 3,849.5 ns | 114.01 ns | 156.06 ns |  1.10 |    0.12 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 2           | 10          | 3,913.8 ns | 170.92 ns | 250.53 ns |  1.11 |    0.13 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 2           | 10          | 3,647.1 ns | 162.06 ns | 227.19 ns |  1.04 |    0.12 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 2           | 10          |   535.5 ns |  35.52 ns |  48.62 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 2           | 10          | 3,552.6 ns | 279.98 ns | 383.24 ns |  1.01 |    0.15 |     440 B |        1.00 |
