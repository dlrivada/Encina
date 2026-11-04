```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   560.2 ns |    114.31 ns | 101.33 ns |  0.16 |    0.03 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   475.8 ns |     20.98 ns |  17.52 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,299.9 ns |    379.54 ns | 316.93 ns |  1.23 |    0.16 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,755.8 ns |    173.09 ns | 144.54 ns |  1.08 |    0.12 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,578.2 ns |    338.87 ns | 282.97 ns |  1.03 |    0.13 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   488.0 ns |    132.72 ns | 110.82 ns |  0.14 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,533.1 ns |    496.60 ns | 440.23 ns |  1.01 |    0.16 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   515.0 ns |    742.63 ns |  40.71 ns |  0.14 |    0.02 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   669.0 ns |  4,568.48 ns | 250.41 ns |  0.18 |    0.06 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,348.2 ns |  7,629.29 ns | 418.19 ns |  1.18 |    0.14 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,898.0 ns |  1,424.88 ns |  78.10 ns |  1.05 |    0.09 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 4,513.2 ns | 17,932.61 ns | 982.95 ns |  1.22 |    0.25 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   533.0 ns |  2,740.02 ns | 150.19 ns |  0.14 |    0.04 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,720.3 ns |  6,769.24 ns | 371.04 ns |  1.01 |    0.12 |     440 B |        1.00 |
