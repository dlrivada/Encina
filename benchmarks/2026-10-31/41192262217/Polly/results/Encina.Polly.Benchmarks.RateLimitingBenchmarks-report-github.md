```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.92GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   487.8 ns |    128.46 ns |   100.29 ns |   471.0 ns |  0.14 |    0.03 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   456.1 ns |     40.89 ns |    34.14 ns |   456.0 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,917.0 ns |    101.40 ns |    84.67 ns | 3,927.0 ns |  1.09 |    0.08 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,817.9 ns |    116.66 ns |   103.42 ns | 3,791.0 ns |  1.06 |    0.08 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,432.9 ns |    112.64 ns |    94.06 ns | 3,446.0 ns |  0.95 |    0.07 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   553.8 ns |     53.59 ns |    47.51 ns |   542.0 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,617.9 ns |    291.38 ns |   272.56 ns | 3,497.0 ns |  1.01 |    0.10 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   714.8 ns |  8,038.29 ns |   440.61 ns |   501.5 ns |  0.19 |    0.10 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   517.0 ns |  1,496.32 ns |    82.02 ns |   480.0 ns |  0.14 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,948.3 ns |  3,008.30 ns |   164.89 ns | 4,039.0 ns |  1.04 |    0.06 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 5,209.8 ns | 44,486.21 ns | 2,438.44 ns | 3,867.5 ns |  1.37 |    0.56 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,699.0 ns |    943.93 ns |    51.74 ns | 3,712.0 ns |  0.97 |    0.05 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   643.0 ns |  5,435.80 ns |   297.95 ns |   476.0 ns |  0.17 |    0.07 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,814.3 ns |  3,724.59 ns |   204.16 ns | 3,767.0 ns |  1.00 |    0.07 |     440 B |        1.00 |
