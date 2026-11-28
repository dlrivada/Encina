```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   696.63 ns |    674.13 ns |   630.58 ns |   800.5 ns |  0.57 |    0.58 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   108.00 ns |     55.89 ns |    49.55 ns |   115.0 ns |  0.09 |    0.05 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           |   730.31 ns |    185.21 ns |   154.66 ns |   771.0 ns |  0.60 |    0.27 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           |   842.33 ns |    163.42 ns |   127.59 ns |   812.0 ns |  0.69 |    0.29 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,004.23 ns |    378.94 ns |   316.44 ns | 2,068.0 ns |  1.63 |    0.69 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    87.57 ns |     34.45 ns |    30.54 ns |   100.5 ns |  0.07 |    0.04 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 1,499.10 ns |    805.20 ns |   753.19 ns | 1,076.5 ns |  1.22 |    0.80 |     440 B |        1.00 |
|                                     |            |                |             |             |             |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           | 1,167.00 ns |  3,661.99 ns |   200.73 ns | 1,217.0 ns |  0.37 |    0.11 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   300.33 ns |  4,141.78 ns |   227.02 ns |   270.0 ns |  0.10 |    0.07 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 2,225.00 ns | 21,560.34 ns | 1,181.79 ns | 1,918.0 ns |  0.71 |    0.38 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 2,066.33 ns | 25,680.05 ns | 1,407.61 ns | 1,612.0 ns |  0.66 |    0.43 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,441.17 ns | 11,809.12 ns |   647.30 ns | 3,274.5 ns |  1.10 |    0.33 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   265.33 ns |  3,969.86 ns |   217.60 ns |   155.0 ns |  0.08 |    0.07 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,305.67 ns | 15,831.54 ns |   867.78 ns | 3,426.0 ns |  1.05 |    0.36 |     440 B |        1.00 |
