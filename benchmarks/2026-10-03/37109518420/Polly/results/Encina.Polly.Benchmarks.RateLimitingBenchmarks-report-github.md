```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   353.2 ns |    89.54 ns |  83.75 ns |  0.14 |    0.03 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   313.7 ns |    26.85 ns |  22.42 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,736.4 ns |    84.03 ns |  70.17 ns |  1.11 |    0.08 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,748.2 ns |   246.31 ns | 230.40 ns |  1.12 |    0.12 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,703.3 ns |   197.14 ns | 184.41 ns |  1.10 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   332.6 ns |    48.21 ns |  42.74 ns |  0.14 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,469.8 ns |   181.70 ns | 169.97 ns |  1.00 |    0.10 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   472.2 ns | 1,319.78 ns |  72.34 ns |  0.17 |    0.03 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   376.0 ns |   482.68 ns |  26.46 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,104.7 ns | 2,871.31 ns | 157.39 ns |  1.12 |    0.11 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,037.0 ns | 2,617.00 ns | 143.45 ns |  1.09 |    0.11 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 2,714.0 ns | 6,003.82 ns | 329.09 ns |  0.98 |    0.13 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   434.0 ns | 1,844.96 ns | 101.13 ns |  0.16 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,798.8 ns | 5,299.49 ns | 290.48 ns |  1.01 |    0.13 |     440 B |        1.00 |
