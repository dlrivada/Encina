```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev       | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|--------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |    961.7 ns |     830.56 ns |    776.91 ns |   402.0 ns |  0.47 |    0.37 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |  1,319.9 ns |     813.86 ns |    761.29 ns | 1,850.0 ns |  0.64 |    0.36 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           |  4,026.7 ns |     532.26 ns |    444.46 ns | 3,965.5 ns |  1.96 |    0.23 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           |  1,841.2 ns |      92.81 ns |     77.50 ns | 1,835.0 ns |  0.89 |    0.06 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |  4,281.2 ns |     245.71 ns |    205.18 ns | 4,322.0 ns |  2.08 |    0.14 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    137.0 ns |      40.80 ns |     36.17 ns |   120.5 ns |  0.07 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           |  2,064.4 ns |     134.82 ns |    105.26 ns | 2,031.5 ns |  1.00 |    0.07 |     440 B |        1.00 |
|                                     |            |                |             |             |             |               |              |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |  1,458.0 ns |   8,075.83 ns |    442.66 ns | 1,532.0 ns |  0.47 |    0.22 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |  1,030.5 ns |  22,832.30 ns |  1,251.52 ns |   324.5 ns |  0.33 |    0.39 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           |  5,323.5 ns |  30,701.30 ns |  1,682.84 ns | 4,769.5 ns |  1.70 |    0.83 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           |  5,666.7 ns |  33,204.90 ns |  1,820.07 ns | 4,835.0 ns |  1.81 |    0.89 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 12,655.0 ns | 249,840.86 ns | 13,694.62 ns | 5,269.0 ns |  4.04 |    4.33 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |  1,067.5 ns |  24,936.17 ns |  1,366.84 ns |   304.5 ns |  0.34 |    0.42 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           |  3,674.3 ns |  34,071.97 ns |  1,867.60 ns | 3,087.0 ns |  1.17 |    0.72 |     440 B |        1.00 |
