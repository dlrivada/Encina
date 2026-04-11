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
| RecordFailure                       | Job-IAMMPO | Default     | 5           |   567.4 ns |  33.21 ns |  29.44 ns |  0.17 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | Default     | 5           |   533.5 ns |  53.74 ns |  50.27 ns |  0.16 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | Default     | 5           | 3,930.2 ns | 100.60 ns |  84.00 ns |  1.18 |    0.04 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | Default     | 5           | 3,723.3 ns | 129.79 ns | 115.06 ns |  1.12 |    0.04 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | Default     | 5           | 3,501.0 ns |  89.77 ns |  83.97 ns |  1.05 |    0.04 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | Default     | 5           |   536.8 ns |  52.67 ns |  49.27 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | Default     | 5           | 3,339.8 ns | 114.14 ns |  95.31 ns |  1.00 |    0.04 |     440 B |        1.00 |
|                                     |            |             |             |            |           |           |       |         |           |             |
| RecordFailure                       | MediumRun  | 2           | 10          |   472.6 ns |  28.03 ns |  41.09 ns |  0.14 |    0.01 |         - |        0.00 |
| GetState                            | MediumRun  | 2           | 10          |   648.8 ns | 130.33 ns | 195.06 ns |  0.20 |    0.06 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 2           | 10          | 3,789.3 ns |  76.37 ns | 111.94 ns |  1.15 |    0.07 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 2           | 10          | 4,040.9 ns | 272.28 ns | 390.50 ns |  1.22 |    0.13 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 2           | 10          | 3,527.2 ns |  51.71 ns |  74.16 ns |  1.07 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 2           | 10          |   491.3 ns |  21.14 ns |  30.32 ns |  0.15 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 2           | 10          | 3,315.0 ns | 129.91 ns | 168.92 ns |  1.00 |    0.07 |     440 B |        1.00 |
