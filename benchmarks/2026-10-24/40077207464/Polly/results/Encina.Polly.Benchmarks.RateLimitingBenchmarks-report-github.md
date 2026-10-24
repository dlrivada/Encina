```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error         | StdDev       | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|--------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   366.6 ns |      37.22 ns |     33.00 ns |   360.5 ns |  0.18 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   453.1 ns |      38.36 ns |     34.01 ns |   451.0 ns |  0.22 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,777.5 ns |     196.71 ns |    174.38 ns | 2,748.5 ns |  1.33 |    0.10 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,806.6 ns |     367.01 ns |    343.30 ns | 2,714.5 ns |  1.35 |    0.17 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,356.6 ns |     104.96 ns |     93.05 ns | 2,329.0 ns |  1.13 |    0.07 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   345.0 ns |     121.61 ns |    113.76 ns |   320.5 ns |  0.17 |    0.05 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,087.7 ns |     131.02 ns |    109.41 ns | 2,058.0 ns |  1.00 |    0.07 |     440 B |        1.00 |
|                                     |            |                |             |             |            |               |              |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   412.2 ns |     421.32 ns |     23.09 ns |   425.5 ns |  0.15 |    0.01 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   415.5 ns |   1,109.72 ns |     60.83 ns |   385.5 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 7,405.5 ns | 149,695.84 ns |  8,205.33 ns | 2,768.5 ns |  2.69 |    2.59 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 8,687.5 ns | 188,796.75 ns | 10,348.59 ns | 2,878.5 ns |  3.16 |    3.27 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 2,817.3 ns |   9,279.48 ns |    508.64 ns | 2,664.0 ns |  1.03 |    0.17 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   411.0 ns |     315.99 ns |     17.32 ns |   421.0 ns |  0.15 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,753.5 ns |   2,808.59 ns |    153.95 ns | 2,713.5 ns |  1.00 |    0.07 |     440 B |        1.00 |
