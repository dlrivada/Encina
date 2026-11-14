```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   570.3 ns |    162.93 ns | 152.41 ns |   501.0 ns |  0.17 |    0.05 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   495.0 ns |    151.74 ns | 134.52 ns |   444.5 ns |  0.15 |    0.04 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,680.5 ns |    248.02 ns | 232.00 ns | 3,580.0 ns |  1.11 |    0.13 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,761.4 ns |    227.94 ns | 202.06 ns | 3,685.5 ns |  1.13 |    0.13 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,266.9 ns |    358.46 ns | 317.76 ns | 3,199.5 ns |  0.98 |    0.14 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   246.4 ns |     71.80 ns |  63.65 ns |   255.5 ns |  0.07 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,350.1 ns |    367.93 ns | 344.17 ns | 3,450.5 ns |  1.01 |    0.14 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   501.0 ns |  1,109.72 ns |  60.83 ns |   471.0 ns |  0.14 |    0.02 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   565.5 ns |  4,770.34 ns | 261.48 ns |   435.5 ns |  0.16 |    0.07 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,779.0 ns |  5,539.22 ns | 303.62 ns | 3,725.0 ns |  1.06 |    0.12 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,751.3 ns | 10,155.22 ns | 556.64 ns | 3,905.0 ns |  1.05 |    0.17 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,556.8 ns | 10,439.83 ns | 572.24 ns | 3,459.5 ns |  0.99 |    0.17 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   448.5 ns |  3,041.13 ns | 166.69 ns |   425.5 ns |  0.13 |    0.04 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,609.0 ns |  7,345.40 ns | 402.63 ns | 3,525.0 ns |  1.01 |    0.14 |     440 B |        1.00 |
