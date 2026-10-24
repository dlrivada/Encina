```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   575.1 ns |     57.26 ns |    47.81 ns |   580.0 ns |  0.16 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   551.5 ns |     69.86 ns |    58.34 ns |   522.0 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,675.0 ns |    127.77 ns |   106.69 ns | 3,677.0 ns |  1.03 |    0.09 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,865.9 ns |    197.65 ns |   175.21 ns | 3,792.0 ns |  1.08 |    0.10 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,482.5 ns |    184.91 ns |   163.92 ns | 3,421.0 ns |  0.97 |    0.09 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   477.5 ns |     31.89 ns |    26.63 ns |   481.0 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,599.1 ns |    348.08 ns |   308.56 ns | 3,521.0 ns |  1.01 |    0.12 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   509.2 ns |    516.12 ns |    28.29 ns |   525.5 ns |  0.12 |    0.02 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   697.3 ns |  6,574.36 ns |   360.36 ns |   520.0 ns |  0.16 |    0.08 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,733.8 ns |  6,014.08 ns |   329.65 ns | 4,623.5 ns |  1.10 |    0.22 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,331.7 ns | 10,918.24 ns |   598.47 ns | 4,269.0 ns |  1.01 |    0.23 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,926.8 ns |  4,527.25 ns |   248.15 ns | 3,836.5 ns |  0.92 |    0.18 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   421.2 ns |  1,376.20 ns |    75.43 ns |   430.5 ns |  0.10 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 4,457.2 ns | 20,846.64 ns | 1,142.67 ns | 3,812.5 ns |  1.04 |    0.31 |     440 B |        1.00 |
