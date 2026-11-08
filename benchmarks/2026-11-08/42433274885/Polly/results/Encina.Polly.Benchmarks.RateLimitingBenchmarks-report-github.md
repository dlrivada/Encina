```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                              | Job        | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |------------ |------------ |-----------:|----------:|----------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | Default     | 5           |   372.2 ns |  22.63 ns |  17.67 ns |   380.5 ns |  0.09 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | Default     | 5           |   538.6 ns |  44.80 ns |  41.91 ns |   545.5 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | Default     | 5           | 3,831.9 ns | 285.31 ns | 252.92 ns | 3,758.0 ns |  0.91 |    0.07 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | Default     | 5           | 3,885.4 ns | 108.91 ns |  90.94 ns | 3,907.0 ns |  0.93 |    0.04 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | Default     | 5           | 3,615.5 ns | 332.69 ns | 311.20 ns | 3,491.5 ns |  0.86 |    0.08 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | Default     | 5           |   512.5 ns |  52.35 ns |  46.41 ns |   510.0 ns |  0.12 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | Default     | 5           | 4,197.9 ns | 217.75 ns | 181.83 ns | 4,137.0 ns |  1.00 |    0.06 |     440 B |        1.00 |
|                                     |            |             |             |            |           |           |            |       |         |           |             |
| RecordFailure                       | MediumRun  | 2           | 10          |   460.9 ns |  38.61 ns |  55.37 ns |   466.0 ns |  0.13 |    0.02 |         - |        0.00 |
| GetState                            | MediumRun  | 2           | 10          |   596.7 ns | 151.01 ns | 211.69 ns |   481.0 ns |  0.17 |    0.06 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 2           | 10          | 4,013.6 ns | 216.01 ns | 302.81 ns | 4,073.0 ns |  1.15 |    0.10 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 2           | 10          | 3,786.0 ns |  87.66 ns | 122.89 ns | 3,747.0 ns |  1.08 |    0.07 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 2           | 10          | 3,480.5 ns |  84.35 ns | 115.45 ns | 3,469.5 ns |  0.99 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 2           | 10          |   614.0 ns | 147.56 ns | 196.98 ns |   502.0 ns |  0.18 |    0.06 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 2           | 10          | 3,513.2 ns | 132.65 ns | 194.44 ns | 3,496.0 ns |  1.00 |    0.08 |     440 B |        1.00 |
