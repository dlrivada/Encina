```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   440.1 ns |    20.37 ns |  15.90 ns |  0.13 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   532.3 ns |    36.06 ns |  31.97 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,713.1 ns |   191.46 ns | 179.09 ns |  1.12 |    0.08 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,574.8 ns |   465.88 ns | 363.73 ns |  1.38 |    0.13 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,506.1 ns |    78.75 ns |  61.48 ns |  1.06 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   746.5 ns |   189.70 ns | 177.44 ns |  0.22 |    0.05 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,329.9 ns |   204.25 ns | 181.06 ns |  1.00 |    0.07 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   514.0 ns | 1,042.77 ns |  57.16 ns |  0.15 |    0.02 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   574.2 ns |   926.73 ns |  50.80 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,448.2 ns | 6,364.17 ns | 348.84 ns |  1.27 |    0.10 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,003.7 ns | 1,771.93 ns |  97.13 ns |  1.15 |    0.05 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,604.8 ns | 1,241.82 ns |  68.07 ns |  1.03 |    0.05 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   441.0 ns |   795.23 ns |  43.59 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,500.2 ns | 3,048.41 ns | 167.09 ns |  1.00 |    0.06 |     440 B |        1.00 |
