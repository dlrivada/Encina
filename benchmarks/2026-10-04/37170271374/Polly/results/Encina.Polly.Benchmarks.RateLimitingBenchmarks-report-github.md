```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.57GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                              | Job        | LaunchCount | WarmupCount | Mean       | Error       | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |------------ |------------ |-----------:|------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | Default     | 5           |   865.1 ns |   877.79 ns |   821.08 ns |   359.0 ns |  0.42 |    0.43 |         - |        0.00 |
| GetState                            | Job-IAMMPO | Default     | 5           |   208.4 ns |    66.52 ns |    55.54 ns |   208.5 ns |  0.10 |    0.04 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | Default     | 5           | 2,333.1 ns |   409.93 ns |   363.39 ns | 2,291.0 ns |  1.12 |    0.41 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | Default     | 5           | 1,942.3 ns |   447.67 ns |   396.85 ns | 1,752.5 ns |  0.93 |    0.36 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | Default     | 5           | 2,684.8 ns | 1,325.31 ns | 1,239.70 ns | 1,892.0 ns |  1.29 |    0.74 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | Default     | 5           | 1,037.1 ns |   977.06 ns |   913.95 ns |   745.0 ns |  0.50 |    0.48 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | Default     | 5           | 2,479.7 ns | 1,346.04 ns | 1,259.09 ns | 1,825.0 ns |  1.19 |    0.73 |     440 B |        1.00 |
|                                     |            |             |             |            |             |             |            |       |         |           |             |
| RecordFailure                       | MediumRun  | 2           | 10          |   562.4 ns |   549.50 ns |   788.07 ns |   141.0 ns |  0.29 |    0.41 |         - |        0.00 |
| GetState                            | MediumRun  | 2           | 10          |   237.8 ns |    63.27 ns |    84.47 ns |   229.0 ns |  0.12 |    0.05 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | MediumRun  | 2           | 10          | 1,999.9 ns |   401.65 ns |   536.19 ns | 1,837.5 ns |  1.03 |    0.31 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | MediumRun  | 2           | 10          | 1,911.8 ns |   650.08 ns |   867.83 ns | 1,617.5 ns |  0.99 |    0.46 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | MediumRun  | 2           | 10          | 3,248.8 ns |   828.17 ns | 1,160.98 ns | 2,682.0 ns |  1.68 |    0.64 |     440 B |        1.00 |
| RecordSuccess                       | MediumRun  | 2           | 10          |   186.2 ns |    73.40 ns |   105.27 ns |   186.8 ns |  0.10 |    0.06 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | MediumRun  | 2           | 10          | 1,983.3 ns |   257.08 ns |   343.20 ns | 1,891.5 ns |  1.02 |    0.23 |     440 B |        1.00 |
