```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|--------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |    86.77 ns |     38.34 ns |    32.02 ns |    86.0000 ns | 0.032 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |    36.00 ns |     21.61 ns |    16.87 ns |    35.5000 ns | 0.013 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           |   786.27 ns |    176.75 ns |   147.60 ns |   790.5000 ns | 0.292 |    0.06 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,552.50 ns |    378.96 ns |   335.94 ns | 2,449.0000 ns | 0.948 |    0.15 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,408.71 ns |    250.12 ns |   221.72 ns | 2,293.5000 ns | 0.895 |    0.12 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    18.00 ns |     34.90 ns |    27.25 ns |     0.0000 ns | 0.007 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,721.43 ns |    338.17 ns |   299.78 ns | 2,604.0000 ns | 1.011 |    0.15 |     440 B |        1.00 |
|                                     |            |                |             |             |             |              |             |               |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   217.00 ns |  4,180.04 ns |   229.12 ns |   100.0000 ns |  0.11 |    0.12 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   655.83 ns | 16,933.89 ns |   928.20 ns |   134.5000 ns |  0.33 |    0.46 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 1,919.67 ns | 23,878.49 ns | 1,308.86 ns | 1,362.0000 ns |  0.97 |    0.76 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,018.33 ns | 24,573.54 ns | 1,346.96 ns | 2,885.0000 ns |  1.53 |    0.93 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,256.33 ns | 13,037.32 ns |   714.62 ns | 2,869.0000 ns |  1.65 |    0.80 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           | 1,128.67 ns | 15,099.11 ns |   827.63 ns | 1,503.0000 ns |  0.57 |    0.47 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,297.00 ns | 17,946.01 ns |   983.68 ns | 2,434.0000 ns |  1.17 |    0.69 |     440 B |        1.00 |
