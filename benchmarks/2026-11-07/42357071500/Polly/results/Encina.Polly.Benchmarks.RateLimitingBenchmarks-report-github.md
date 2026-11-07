```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   435.0 ns |    46.98 ns |  41.65 ns |  0.13 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   490.5 ns |    27.44 ns |  25.67 ns |  0.15 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,631.9 ns |    93.04 ns |  77.69 ns |  1.09 |    0.03 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,628.7 ns |    78.21 ns |  69.33 ns |  1.09 |    0.03 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,426.0 ns |    69.14 ns |  61.29 ns |  1.03 |    0.03 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   405.4 ns |    30.87 ns |  27.37 ns |  0.12 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,332.1 ns |    85.51 ns |  71.40 ns |  1.00 |    0.03 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   641.0 ns | 4,750.41 ns | 260.39 ns |  0.18 |    0.06 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   554.7 ns |   270.80 ns |  14.84 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,011.3 ns | 5,195.28 ns | 284.77 ns |  1.14 |    0.09 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,860.5 ns | 2,113.20 ns | 115.83 ns |  1.09 |    0.06 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,588.2 ns | 3,285.56 ns | 180.09 ns |  1.02 |    0.07 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   461.0 ns | 1,377.37 ns |  75.50 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,537.3 ns | 3,986.59 ns | 218.52 ns |  1.00 |    0.07 |     440 B |        1.00 |
