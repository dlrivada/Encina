```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   392.5 ns |     50.98 ns |    42.57 ns |  0.11 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   406.9 ns |    106.35 ns |    88.81 ns |  0.11 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,880.5 ns |    182.67 ns |   152.54 ns |  1.05 |    0.11 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,571.2 ns |    265.57 ns |   235.42 ns |  1.24 |    0.13 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,565.3 ns |    188.40 ns |   167.02 ns |  0.97 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   413.2 ns |     43.24 ns |    33.76 ns |  0.11 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,718.9 ns |    427.75 ns |   379.19 ns |  1.01 |    0.14 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   644.3 ns |    640.70 ns |    35.12 ns |  0.16 |    0.01 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   523.3 ns |  1,834.41 ns |   100.55 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,291.0 ns |  3,794.39 ns |   207.98 ns |  1.07 |    0.07 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 5,055.3 ns | 21,419.59 ns | 1,174.08 ns |  1.25 |    0.26 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,641.8 ns |  3,491.20 ns |   191.36 ns |  0.90 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   516.3 ns |  3,841.61 ns |   210.57 ns |  0.13 |    0.05 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 4,039.0 ns |  4,599.70 ns |   252.12 ns |  1.00 |    0.08 |     440 B |        1.00 |
