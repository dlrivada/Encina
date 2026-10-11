```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                              | Job        | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | Default     | 5           |   579.9 ns |  34.43 ns |  30.52 ns |  0.15 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | Default     | 5           |   518.1 ns |  37.53 ns |  35.11 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | Default     | 5           | 3,788.2 ns |  77.86 ns |  69.02 ns |  0.98 |    0.09 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | Default     | 5           | 3,856.7 ns | 125.68 ns | 104.95 ns |  1.00 |    0.10 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | Default     | 5           | 3,395.1 ns |  78.60 ns |  65.64 ns |  0.88 |    0.08 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | Default     | 5           |   472.6 ns |  26.38 ns |  24.68 ns |  0.12 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | Default     | 5           | 3,885.9 ns | 401.89 ns | 375.93 ns |  1.01 |    0.13 |     440 B |        1.00 |
|                                     |            |             |             |            |           |           |       |         |           |             |
| RecordFailure                       | MediumRun  | 2           | 10          |   531.0 ns |  31.18 ns |  45.70 ns |  0.15 |    0.01 |         - |        0.00 |
| GetState                            | MediumRun  | 2           | 10          |   537.0 ns |  12.74 ns |  17.44 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 2           | 10          | 4,525.1 ns | 188.18 ns | 263.81 ns |  1.32 |    0.08 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 2           | 10          | 3,808.2 ns |  62.98 ns |  92.32 ns |  1.11 |    0.03 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 2           | 10          | 3,563.6 ns |  46.11 ns |  64.64 ns |  1.04 |    0.03 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 2           | 10          |   487.6 ns |  22.49 ns |  31.53 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 2           | 10          | 3,439.0 ns |  49.84 ns |  69.87 ns |  1.00 |    0.03 |     440 B |        1.00 |
