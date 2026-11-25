```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   327.0 ns |     60.49 ns |    53.62 ns |   330.5 ns |  0.07 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   221.7 ns |     36.16 ns |    30.19 ns |   219.0 ns |  0.05 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,784.0 ns |    988.63 ns |   876.40 ns | 4,043.5 ns |  0.80 |    0.18 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,820.7 ns |    517.25 ns |   431.93 ns | 2,587.5 ns |  0.59 |    0.09 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 4,560.4 ns |    191.73 ns |   160.10 ns | 4,581.0 ns |  0.96 |    0.05 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   164.4 ns |     16.75 ns |    14.85 ns |   165.5 ns |  0.03 |    0.00 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 4,756.8 ns |    251.86 ns |   210.32 ns | 4,695.0 ns |  1.00 |    0.06 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           | 1,129.2 ns | 25,758.50 ns | 1,411.91 ns |   314.5 ns |  0.32 |    0.38 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   326.2 ns |  6,480.56 ns |   355.22 ns |   171.5 ns |  0.09 |    0.10 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,043.5 ns | 27,625.24 ns | 1,514.23 ns | 3,938.5 ns |  1.16 |    0.55 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,889.8 ns | 26,757.54 ns | 1,466.67 ns | 3,251.5 ns |  1.11 |    0.53 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,819.7 ns | 30,182.20 ns | 1,654.39 ns | 3,235.0 ns |  1.09 |    0.56 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   560.3 ns |  5,440.68 ns |   298.22 ns |   450.0 ns |  0.16 |    0.09 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,948.2 ns | 32,977.97 ns | 1,807.63 ns | 3,207.5 ns |  1.13 |    0.60 |     440 B |        1.00 |
