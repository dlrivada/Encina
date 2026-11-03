```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   371.0 ns |    55.49 ns |  51.90 ns |  0.13 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   369.7 ns |    50.67 ns |  44.92 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,318.5 ns |   279.24 ns | 247.54 ns |  1.16 |    0.09 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,512.2 ns |   238.45 ns | 223.05 ns |  1.23 |    0.09 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,971.7 ns |   249.93 ns | 208.70 ns |  1.04 |    0.08 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   350.0 ns |    24.68 ns |  19.27 ns |  0.12 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,852.1 ns |   122.24 ns | 102.08 ns |  1.00 |    0.05 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   456.0 ns |   631.98 ns |  34.64 ns |  0.14 |    0.01 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   520.0 ns |   965.37 ns |  52.92 ns |  0.16 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,870.5 ns | 3,861.47 ns | 211.66 ns |  1.15 |    0.10 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,635.7 ns | 6,054.34 ns | 331.86 ns |  1.08 |    0.12 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,489.8 ns | 7,130.95 ns | 390.87 ns |  1.04 |    0.13 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   520.5 ns | 2,278.64 ns | 124.90 ns |  0.16 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,371.5 ns | 5,411.87 ns | 296.64 ns |  1.01 |    0.11 |     440 B |        1.00 |
