```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   543.1 ns |   115.30 ns | 107.85 ns |  0.24 |    0.05 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   319.8 ns |    25.66 ns |  21.42 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,788.6 ns |   183.80 ns | 162.94 ns |  1.21 |    0.08 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,198.4 ns |   454.74 ns | 403.11 ns |  0.95 |    0.17 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,528.3 ns |   284.71 ns | 266.31 ns |  1.10 |    0.12 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   311.5 ns |    58.81 ns |  49.11 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,311.7 ns |   121.26 ns | 101.25 ns |  1.00 |    0.06 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   389.3 ns | 1,806.99 ns |  99.05 ns |  0.14 |    0.03 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   398.5 ns | 2,180.64 ns | 119.53 ns |  0.15 |    0.04 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 2,985.0 ns | 7,177.93 ns | 393.45 ns |  1.10 |    0.15 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,327.7 ns | 8,703.05 ns | 477.04 ns |  1.23 |    0.17 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 2,552.0 ns | 7,267.94 ns | 398.38 ns |  0.94 |    0.14 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   323.5 ns | 2,241.60 ns | 122.87 ns |  0.12 |    0.04 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,721.2 ns | 3,901.48 ns | 213.85 ns |  1.00 |    0.10 |     440 B |        1.00 |
