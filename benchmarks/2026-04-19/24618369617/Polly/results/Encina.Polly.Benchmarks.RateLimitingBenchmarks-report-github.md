```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                              | Job        | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | Default     | 5           |   702.1 ns | 189.50 ns | 177.26 ns |  0.21 |    0.05 |         - |        0.00 |
| GetState                            | Job-IAMMPO | Default     | 5           |   668.5 ns | 147.49 ns | 137.96 ns |  0.20 |    0.04 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | Default     | 5           | 3,839.4 ns | 326.19 ns | 289.16 ns |  1.12 |    0.08 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | Default     | 5           | 4,782.4 ns | 332.27 ns | 294.55 ns |  1.40 |    0.09 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | Default     | 5           | 3,406.6 ns | 228.29 ns | 213.54 ns |  1.00 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | Default     | 5           |   568.4 ns |  40.19 ns |  33.56 ns |  0.17 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | Default     | 5           | 3,415.4 ns |  89.62 ns |  69.97 ns |  1.00 |    0.03 |     440 B |        1.00 |
|                                     |            |             |             |            |           |           |       |         |           |             |
| RecordFailure                       | MediumRun  | 2           | 10          |   555.9 ns |  33.71 ns |  46.14 ns |  0.14 |    0.02 |         - |        0.00 |
| GetState                            | MediumRun  | 2           | 10          |   532.9 ns |  43.31 ns |  56.31 ns |  0.14 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 2           | 10          | 4,284.3 ns | 229.55 ns | 329.21 ns |  1.11 |    0.17 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 2           | 10          | 3,660.8 ns |  92.99 ns | 130.36 ns |  0.95 |    0.13 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 2           | 10          | 3,649.9 ns | 245.48 ns | 344.13 ns |  0.94 |    0.15 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 2           | 10          |   529.7 ns |  21.21 ns |  29.74 ns |  0.14 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 2           | 10          | 3,928.4 ns | 360.10 ns | 516.44 ns |  1.02 |    0.19 |     440 B |        1.00 |
