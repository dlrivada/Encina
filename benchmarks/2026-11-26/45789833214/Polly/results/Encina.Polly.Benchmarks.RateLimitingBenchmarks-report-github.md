```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.35GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev       | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|--------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |    28.58 ns |     14.131 ns |    11.033 ns |    30.00 ns |  0.03 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |    59.43 ns |     34.098 ns |    30.227 ns |    45.00 ns |  0.07 |    0.04 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           |   752.54 ns |    120.892 ns |   100.950 ns |   741.00 ns |  0.86 |    0.20 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,276.54 ns |    677.917 ns |   566.091 ns | 2,494.00 ns |  2.61 |    0.82 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |   758.00 ns |    102.262 ns |    85.393 ns |   751.00 ns |  0.87 |    0.20 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    45.92 ns |      6.473 ns |     5.054 ns |    50.00 ns |  0.05 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           |   915.57 ns |    254.259 ns |   225.394 ns |   821.00 ns |  1.05 |    0.33 |     440 B |        1.00 |
|                                     |            |                |             |             |             |               |              |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           | 1,112.83 ns | 17,806.213 ns |   976.019 ns | 1,306.50 ns |  0.66 |    0.57 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   210.00 ns |  4,582.771 ns |   251.197 ns |    70.00 ns |  0.12 |    0.14 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 2,148.33 ns | 19,618.682 ns | 1,075.366 ns | 1,538.00 ns |  1.27 |    0.73 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 1,846.00 ns | 22,090.640 ns | 1,210.863 ns | 1,362.00 ns |  1.09 |    0.75 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 1,651.00 ns | 22,984.709 ns | 1,259.869 ns | 1,297.00 ns |  0.97 |    0.76 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   382.33 ns |  2,661.354 ns |   145.878 ns |   335.00 ns |  0.23 |    0.11 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 1,952.83 ns | 17,712.412 ns |   970.877 ns | 1,522.50 ns |  1.15 |    0.66 |     440 B |        1.00 |
