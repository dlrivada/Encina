```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                              | Job        | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | Default     | 5           |   555.6 ns |  38.01 ns |  35.56 ns |  0.16 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | Default     | 5           |   525.7 ns |  57.10 ns |  44.58 ns |  0.15 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | Default     | 5           | 3,867.5 ns | 195.76 ns | 173.54 ns |  1.08 |    0.07 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | Default     | 5           | 4,021.4 ns | 189.38 ns | 177.14 ns |  1.13 |    0.07 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | Default     | 5           | 3,379.2 ns |  93.11 ns |  77.75 ns |  0.95 |    0.05 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | Default     | 5           |   565.7 ns |  36.26 ns |  33.92 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | Default     | 5           | 3,572.6 ns | 195.49 ns | 173.30 ns |  1.00 |    0.07 |     440 B |        1.00 |
|                                     |            |             |             |            |           |           |       |         |           |             |
| RecordFailure                       | MediumRun  | 2           | 10          |   541.1 ns |  35.85 ns |  51.42 ns |  0.15 |    0.02 |         - |        0.00 |
| GetState                            | MediumRun  | 2           | 10          |   552.1 ns |  32.40 ns |  48.50 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 2           | 10          | 3,988.8 ns | 177.45 ns | 260.11 ns |  1.14 |    0.08 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 2           | 10          | 3,894.4 ns |  79.93 ns | 114.64 ns |  1.11 |    0.05 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 2           | 10          | 3,620.6 ns |  89.50 ns | 131.18 ns |  1.03 |    0.05 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 2           | 10          |   531.8 ns |  35.12 ns |  50.37 ns |  0.15 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 2           | 10          | 3,503.9 ns |  75.52 ns | 105.87 ns |  1.00 |    0.04 |     440 B |        1.00 |
