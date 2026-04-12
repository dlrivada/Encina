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
| RecordFailure                       | Job-IAMMPO | Default     | 5           |   502.4 ns |  54.51 ns |  50.99 ns |  0.14 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | Default     | 5           |   458.5 ns |  65.10 ns |  60.89 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | Default     | 5           | 3,674.4 ns |  95.19 ns |  84.39 ns |  1.01 |    0.10 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | Default     | 5           | 3,819.6 ns | 107.60 ns | 100.65 ns |  1.05 |    0.11 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | Default     | 5           | 3,573.5 ns |  70.22 ns |  65.69 ns |  0.98 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | Default     | 5           |   480.9 ns |  12.49 ns |  11.69 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | Default     | 5           | 3,678.1 ns | 434.18 ns | 406.13 ns |  1.01 |    0.15 |     440 B |        1.00 |
|                                     |            |             |             |            |           |           |       |         |           |             |
| RecordFailure                       | MediumRun  | 2           | 10          |   468.7 ns |  21.36 ns |  29.94 ns |  0.14 |    0.01 |         - |        0.00 |
| GetState                            | MediumRun  | 2           | 10          |   494.3 ns |  38.40 ns |  56.29 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 2           | 10          | 4,152.7 ns | 280.26 ns | 410.80 ns |  1.24 |    0.12 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 2           | 10          | 4,079.5 ns | 335.49 ns | 459.22 ns |  1.21 |    0.14 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 2           | 10          | 3,542.7 ns | 138.90 ns | 203.60 ns |  1.05 |    0.07 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 2           | 10          |   547.5 ns |  32.68 ns |  47.90 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 2           | 10          | 3,362.0 ns |  59.17 ns |  86.73 ns |  1.00 |    0.04 |     440 B |        1.00 |
