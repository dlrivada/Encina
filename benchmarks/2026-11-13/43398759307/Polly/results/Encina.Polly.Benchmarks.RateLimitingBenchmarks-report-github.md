```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   562.8 ns |   176.05 ns | 156.07 ns |   535.5 ns |  0.16 |    0.04 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   725.3 ns |   197.94 ns | 185.16 ns |   821.0 ns |  0.20 |    0.05 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,894.9 ns |   201.90 ns | 157.63 ns | 3,882.0 ns |  1.10 |    0.05 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,620.2 ns |   107.86 ns |  90.07 ns | 3,627.0 ns |  1.02 |    0.04 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,935.9 ns |   443.87 ns | 415.20 ns | 4,048.0 ns |  1.11 |    0.12 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   523.2 ns |    47.55 ns |  42.15 ns |   515.5 ns |  0.15 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,557.0 ns |   116.02 ns |  96.88 ns | 3,561.5 ns |  1.00 |    0.04 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   606.2 ns | 5,379.90 ns | 294.89 ns |   445.5 ns |  0.17 |    0.07 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   978.5 ns | 2,305.29 ns | 126.36 ns |   961.5 ns |  0.27 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,723.7 ns | 4,197.38 ns | 230.07 ns | 3,717.0 ns |  1.02 |    0.07 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,020.5 ns | 5,170.96 ns | 283.44 ns | 4,067.5 ns |  1.10 |    0.08 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,712.0 ns | 1,740.34 ns |  95.39 ns | 3,662.0 ns |  1.01 |    0.04 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   692.8 ns | 4,327.66 ns | 237.21 ns |   655.5 ns |  0.19 |    0.06 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,662.7 ns | 2,808.84 ns | 153.96 ns | 3,746.0 ns |  1.00 |    0.05 |     440 B |        1.00 |
