```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   428.8 ns |    34.65 ns |  28.93 ns |  0.19 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   356.1 ns |    19.97 ns |  16.68 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,514.0 ns |   120.05 ns | 100.25 ns |  1.11 |    0.09 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,546.1 ns |   182.39 ns | 161.69 ns |  1.13 |    0.10 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,352.2 ns |   128.80 ns | 107.56 ns |  1.04 |    0.08 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   449.1 ns |   103.76 ns |  97.06 ns |  0.20 |    0.04 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,267.9 ns |   180.11 ns | 159.66 ns |  1.00 |    0.10 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   413.0 ns | 2,482.55 ns | 136.08 ns |  0.15 |    0.04 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   426.5 ns | 1,496.32 ns |  82.02 ns |  0.15 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 2,950.8 ns | 5,034.42 ns | 275.95 ns |  1.07 |    0.12 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 2,936.3 ns | 3,155.71 ns | 172.97 ns |  1.06 |    0.09 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 2,893.0 ns | 5,911.56 ns | 324.03 ns |  1.05 |    0.13 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   357.7 ns | 1,369.29 ns |  75.06 ns |  0.13 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,778.0 ns | 4,410.19 ns | 241.74 ns |  1.00 |    0.11 |     440 B |        1.00 |
