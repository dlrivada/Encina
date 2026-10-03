```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   468.2 ns |     40.59 ns |    37.97 ns |  0.14 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   444.0 ns |     29.57 ns |    27.66 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,671.0 ns |     74.88 ns |    62.53 ns |  1.10 |    0.03 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,751.5 ns |     91.78 ns |    85.85 ns |  1.13 |    0.04 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 4,434.1 ns |  1,266.41 ns | 1,122.64 ns |  1.33 |    0.33 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   485.7 ns |     21.71 ns |    19.25 ns |  0.15 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,328.8 ns |     98.72 ns |    82.43 ns |  1.00 |    0.03 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   537.3 ns |    565.36 ns |    30.99 ns |  0.12 |    0.01 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   570.0 ns |    315.99 ns |    17.32 ns |  0.13 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,657.7 ns | 18,442.45 ns | 1,010.89 ns |  1.05 |    0.20 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,874.3 ns |  2,122.89 ns |   116.36 ns |  0.87 |    0.04 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,305.3 ns |  6,877.43 ns |   376.98 ns |  0.75 |    0.08 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   432.2 ns |  1,037.38 ns |    56.86 ns |  0.10 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 4,437.7 ns |  3,281.35 ns |   179.86 ns |  1.00 |    0.05 |     440 B |        1.00 |
