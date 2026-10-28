```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   511.0 ns |     66.56 ns |  62.26 ns |   491.0 ns |  0.19 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   250.7 ns |     54.57 ns |  48.38 ns |   230.5 ns |  0.09 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,561.3 ns |    425.53 ns | 398.04 ns | 3,376.0 ns |  1.30 |    0.15 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,166.7 ns |    224.78 ns | 199.26 ns | 3,134.0 ns |  1.15 |    0.08 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,215.3 ns |    272.09 ns | 241.20 ns | 3,185.0 ns |  1.17 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   559.3 ns |     41.70 ns |  39.01 ns |   550.5 ns |  0.20 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,751.8 ns |    134.41 ns | 112.24 ns | 2,718.5 ns |  1.00 |    0.06 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   560.5 ns |  5,059.14 ns | 277.31 ns |   410.5 ns |  0.16 |    0.07 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   405.5 ns |  3,791.89 ns | 207.85 ns |   285.5 ns |  0.11 |    0.05 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,814.7 ns |  7,777.06 ns | 426.29 ns | 3,631.0 ns |  1.06 |    0.17 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,857.2 ns | 11,163.08 ns | 611.89 ns | 4,060.5 ns |  1.07 |    0.21 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,529.0 ns |  7,319.96 ns | 401.23 ns | 3,676.0 ns |  0.98 |    0.16 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   420.7 ns |  1,325.70 ns |  72.67 ns |   441.0 ns |  0.12 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,670.7 ns | 11,201.72 ns | 614.00 ns | 3,410.0 ns |  1.02 |    0.20 |     440 B |        1.00 |
