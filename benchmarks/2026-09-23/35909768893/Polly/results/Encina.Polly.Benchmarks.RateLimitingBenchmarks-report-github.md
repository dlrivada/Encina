```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   528.1 ns |     90.12 ns |  79.89 ns |   536.0 ns |  0.15 |    0.03 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   457.5 ns |     28.94 ns |  22.60 ns |   461.0 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,008.7 ns |    303.83 ns | 284.20 ns | 3,856.5 ns |  1.12 |    0.13 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,739.6 ns |    282.50 ns | 250.43 ns | 4,698.0 ns |  1.33 |    0.14 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,577.9 ns |    153.28 ns | 119.67 ns | 3,557.0 ns |  1.00 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   606.4 ns |     35.68 ns |  29.79 ns |   610.5 ns |  0.17 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,612.1 ns |    458.51 ns | 406.46 ns | 3,416.5 ns |  1.01 |    0.15 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   564.2 ns |  1,065.40 ns |  58.40 ns |   541.5 ns |  0.15 |    0.02 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   561.7 ns |  1,305.46 ns |  71.56 ns |   542.0 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,513.5 ns | 16,198.03 ns | 887.87 ns | 4,083.5 ns |  1.21 |    0.22 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,956.7 ns |  4,390.65 ns | 240.67 ns | 3,936.0 ns |  1.06 |    0.09 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,776.0 ns |  8,168.24 ns | 447.73 ns | 3,916.0 ns |  1.01 |    0.13 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   594.7 ns |  5,646.43 ns | 309.50 ns |   421.0 ns |  0.16 |    0.07 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,738.5 ns |  5,403.25 ns | 296.17 ns | 3,761.5 ns |  1.00 |    0.10 |     440 B |        1.00 |
