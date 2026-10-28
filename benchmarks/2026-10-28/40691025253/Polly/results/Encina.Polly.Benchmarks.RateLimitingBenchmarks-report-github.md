```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.05GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error         | StdDev       | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|--------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   544.3 ns |      43.92 ns |     41.08 ns |   540.5 ns |  0.13 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   532.4 ns |      62.37 ns |     58.34 ns |   531.5 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,841.4 ns |     222.54 ns |    197.28 ns | 3,832.0 ns |  0.95 |    0.09 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,866.1 ns |     228.19 ns |    190.55 ns | 3,867.0 ns |  0.96 |    0.09 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,525.6 ns |     157.90 ns |    139.97 ns | 3,487.0 ns |  0.87 |    0.07 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   553.4 ns |     152.41 ns |    135.10 ns |   485.0 ns |  0.14 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 4,059.0 ns |     334.08 ns |    312.50 ns | 4,137.0 ns |  1.01 |    0.11 |     440 B |        1.00 |
|                                     |            |                |             |             |            |               |              |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   489.0 ns |     938.62 ns |     51.45 ns |   475.0 ns |  0.14 |    0.01 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           | 9,320.3 ns | 280,136.17 ns | 15,355.21 ns |   460.0 ns |  2.58 |    3.68 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,794.3 ns |   3,138.26 ns |    172.02 ns | 3,768.0 ns |  1.05 |    0.06 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,833.3 ns |   3,750.86 ns |    205.60 ns | 3,947.0 ns |  1.06 |    0.06 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,654.0 ns |   1,558.42 ns |     85.42 ns | 3,687.0 ns |  1.01 |    0.04 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   508.0 ns |     853.17 ns |     46.77 ns |   481.0 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,623.2 ns |   3,076.29 ns |    168.62 ns | 3,546.5 ns |  1.00 |    0.06 |     440 B |        1.00 |
