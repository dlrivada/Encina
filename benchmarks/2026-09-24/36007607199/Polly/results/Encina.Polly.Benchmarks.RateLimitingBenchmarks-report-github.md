```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   478.2 ns |    38.80 ns |  32.40 ns |  0.14 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   531.7 ns |    76.34 ns |  67.68 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,084.0 ns |   137.58 ns | 121.96 ns |  1.17 |    0.06 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,932.1 ns |   201.34 ns | 178.48 ns |  1.13 |    0.07 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,535.7 ns |    78.69 ns |  73.61 ns |  1.02 |    0.05 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   544.1 ns |    37.01 ns |  34.62 ns |  0.16 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,487.2 ns |   223.27 ns | 174.32 ns |  1.00 |    0.07 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   629.2 ns | 2,382.91 ns | 130.62 ns |  0.18 |    0.03 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   570.8 ns | 1,206.07 ns |  66.11 ns |  0.16 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,930.2 ns | 4,287.09 ns | 234.99 ns |  1.10 |    0.09 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,416.5 ns | 8,507.66 ns | 466.33 ns |  1.23 |    0.14 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,738.5 ns | 6,760.20 ns | 370.55 ns |  1.04 |    0.11 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   537.8 ns | 1,004.29 ns |  55.05 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,589.3 ns | 4,484.89 ns | 245.83 ns |  1.00 |    0.09 |     440 B |        1.00 |
