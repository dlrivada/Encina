```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   617.0 ns |    138.20 ns | 122.51 ns |  0.16 |    0.04 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   526.8 ns |     73.03 ns |  60.98 ns |  0.14 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,439.8 ns |    532.35 ns | 471.92 ns |  1.15 |    0.18 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,065.5 ns |    431.22 ns | 403.37 ns |  1.06 |    0.16 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,960.5 ns |    546.09 ns | 484.10 ns |  1.03 |    0.17 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   437.1 ns |     31.21 ns |  26.06 ns |  0.11 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,900.8 ns |    525.76 ns | 491.80 ns |  1.01 |    0.17 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   568.0 ns |    686.75 ns |  37.64 ns |  0.13 |    0.01 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   407.5 ns |    735.88 ns |  40.34 ns |  0.09 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,193.0 ns |  2,385.68 ns | 130.77 ns |  0.96 |    0.04 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,978.7 ns | 14,926.36 ns | 818.16 ns |  1.14 |    0.17 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,944.0 ns |  4,851.15 ns | 265.91 ns |  0.91 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   513.7 ns |  3,119.11 ns | 170.97 ns |  0.12 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 4,355.3 ns |  2,317.05 ns | 127.01 ns |  1.00 |    0.04 |     440 B |        1.00 |
