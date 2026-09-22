```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   478.7 ns |     26.66 ns |    23.63 ns |   485.5 ns |  0.14 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   440.4 ns |     67.96 ns |    60.24 ns |   416.0 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,817.7 ns |     43.05 ns |    35.95 ns | 3,826.5 ns |  1.15 |    0.03 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,734.5 ns |     51.73 ns |    43.20 ns | 3,746.0 ns |  1.13 |    0.03 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,413.2 ns |    142.44 ns |   118.94 ns | 3,386.5 ns |  1.03 |    0.04 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   716.9 ns |    218.32 ns |   204.22 ns |   590.5 ns |  0.22 |    0.06 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,317.4 ns |     97.04 ns |    81.04 ns | 3,336.0 ns |  1.00 |    0.03 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   557.5 ns |    859.01 ns |    47.09 ns |   541.5 ns |  0.14 |    0.02 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   474.5 ns |  2,328.99 ns |   127.66 ns |   410.5 ns |  0.12 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,849.3 ns | 37,599.36 ns | 2,060.95 ns | 3,797.0 ns |  1.22 |    0.49 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,856.3 ns |  2,659.48 ns |   145.77 ns | 3,846.0 ns |  0.97 |    0.15 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,486.0 ns |    364.87 ns |    20.00 ns | 3,486.0 ns |  0.88 |    0.13 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   494.0 ns |    749.33 ns |    41.07 ns |   481.0 ns |  0.12 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 4,057.0 ns | 11,730.74 ns |   643.00 ns | 4,387.0 ns |  1.02 |    0.21 |     440 B |        1.00 |
