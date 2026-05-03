```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                              | Job        | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | Default     | 5           |   419.6 ns |  37.51 ns |  33.25 ns |  0.15 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | Default     | 5           |   684.6 ns | 188.55 ns | 176.37 ns |  0.24 |    0.06 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | Default     | 5           | 3,647.5 ns | 149.16 ns | 116.45 ns |  1.27 |    0.10 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | Default     | 5           | 3,701.3 ns | 208.82 ns | 195.33 ns |  1.29 |    0.11 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | Default     | 5           | 3,049.1 ns | 187.07 ns | 165.83 ns |  1.06 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | Default     | 5           |   433.9 ns |  43.27 ns |  38.36 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | Default     | 5           | 2,884.7 ns | 261.72 ns | 232.01 ns |  1.01 |    0.11 |     440 B |        1.00 |
|                                     |            |             |             |            |           |           |       |         |           |             |
| RecordFailure                       | MediumRun  | 2           | 10          |   335.6 ns |  56.04 ns |  80.37 ns |  0.11 |    0.03 |         - |        0.00 |
| GetState                            | MediumRun  | 2           | 10          |   479.6 ns |  51.60 ns |  77.23 ns |  0.16 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 2           | 10          | 3,356.4 ns | 219.49 ns | 307.70 ns |  1.10 |    0.14 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 2           | 10          | 3,412.1 ns | 176.73 ns | 247.76 ns |  1.12 |    0.13 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 2           | 10          | 3,048.1 ns | 195.57 ns | 274.16 ns |  1.00 |    0.13 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 2           | 10          |   488.4 ns | 110.22 ns | 164.97 ns |  0.16 |    0.06 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 2           | 10          | 3,086.3 ns | 210.16 ns | 308.05 ns |  1.01 |    0.14 |     440 B |        1.00 |
