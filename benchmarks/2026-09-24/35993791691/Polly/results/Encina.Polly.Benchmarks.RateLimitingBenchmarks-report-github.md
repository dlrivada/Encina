```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   440.5 ns |    52.75 ns |  44.04 ns |   432.0 ns |  0.12 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   511.1 ns |    51.76 ns |  48.42 ns |   511.0 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,284.5 ns |   407.40 ns | 381.08 ns | 4,136.0 ns |  1.13 |    0.12 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,865.6 ns |   275.11 ns | 243.88 ns | 3,777.0 ns |  1.02 |    0.09 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,951.9 ns |   405.23 ns | 316.37 ns | 4,013.0 ns |  1.04 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   504.5 ns |    26.80 ns |  22.38 ns |   502.0 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,801.7 ns |   302.06 ns | 252.23 ns | 3,752.0 ns |  1.00 |    0.09 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   697.7 ns | 6,733.75 ns | 369.10 ns |   520.0 ns |  0.16 |    0.08 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   595.3 ns |   737.31 ns |  40.41 ns |   602.0 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,431.3 ns | 9,851.51 ns | 539.99 ns | 4,318.0 ns |  1.04 |    0.13 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,284.8 ns | 7,411.56 ns | 406.25 ns | 4,157.5 ns |  1.00 |    0.11 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 4,384.3 ns | 9,958.84 ns | 545.88 ns | 4,268.0 ns |  1.03 |    0.14 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   514.0 ns | 2,363.04 ns | 129.53 ns |   551.0 ns |  0.12 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 4,289.8 ns | 6,446.01 ns | 353.33 ns | 4,473.5 ns |  1.00 |    0.10 |     440 B |        1.00 |
