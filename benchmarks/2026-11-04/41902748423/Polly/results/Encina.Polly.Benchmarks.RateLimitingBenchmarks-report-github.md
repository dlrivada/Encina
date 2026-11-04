```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev       | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|--------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |    330.9 ns |      50.09 ns |     39.10 ns |   331.0 ns |  0.09 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |    493.6 ns |      25.51 ns |     19.92 ns |   496.0 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           |  3,667.2 ns |      92.70 ns |     77.41 ns | 3,657.0 ns |  1.04 |    0.08 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           |  3,748.7 ns |     131.52 ns |    116.59 ns | 3,777.0 ns |  1.06 |    0.08 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |  3,534.4 ns |     152.27 ns |    142.43 ns | 3,477.0 ns |  1.00 |    0.08 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    499.7 ns |      27.68 ns |     24.53 ns |   491.0 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           |  3,546.3 ns |     332.28 ns |    294.56 ns | 3,452.0 ns |  1.01 |    0.11 |     440 B |        1.00 |
|                                     |            |                |             |             |             |               |              |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |    397.7 ns |   1,950.74 ns |    106.93 ns |   341.0 ns |  0.10 |    0.02 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |    466.7 ns |   2,137.97 ns |    117.19 ns |   420.0 ns |  0.12 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           |  3,794.3 ns |   3,455.75 ns |    189.42 ns | 3,728.0 ns |  0.98 |    0.07 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 12,327.8 ns | 267,048.16 ns | 14,637.81 ns | 4,002.5 ns |  3.18 |    3.28 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           |  3,803.5 ns |     995.41 ns |     54.56 ns | 3,806.5 ns |  0.98 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |    558.3 ns |     893.82 ns |     48.99 ns |   582.0 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           |  3,890.3 ns |   4,598.52 ns |    252.06 ns | 3,917.0 ns |  1.00 |    0.08 |     440 B |        1.00 |
