```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   510.6 ns |     33.66 ns |    31.49 ns |  0.15 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   552.7 ns |     28.13 ns |    26.31 ns |  0.17 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,104.0 ns |    437.39 ns |   387.74 ns |  1.23 |    0.12 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,731.0 ns |     82.64 ns |    73.26 ns |  1.12 |    0.04 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,601.7 ns |     88.81 ns |    74.16 ns |  1.08 |    0.04 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   498.1 ns |     26.43 ns |    24.72 ns |  0.15 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,326.5 ns |    113.48 ns |    88.60 ns |  1.00 |    0.04 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   567.8 ns |  1,955.60 ns |   107.19 ns |  0.13 |    0.04 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   576.2 ns |  1,120.22 ns |    61.40 ns |  0.13 |    0.04 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,074.3 ns |  2,565.20 ns |   140.61 ns |  0.90 |    0.26 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,876.8 ns |  3,961.47 ns |   217.14 ns |  0.86 |    0.25 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,525.7 ns |  3,934.91 ns |   215.69 ns |  0.78 |    0.23 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   514.2 ns |    650.24 ns |    35.64 ns |  0.11 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 4,966.0 ns | 36,952.94 ns | 2,025.52 ns |  1.10 |    0.51 |     440 B |        1.00 |
