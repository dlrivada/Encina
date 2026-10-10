```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev       | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|--------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   100.25 ns |     36.067 ns |    28.159 ns |    90.50 ns |  0.09 |    0.03 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |    18.00 ns |     27.854 ns |    23.259 ns |    10.00 ns |  0.02 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           |   674.29 ns |    343.527 ns |   304.528 ns |   530.50 ns |  0.60 |    0.28 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,104.42 ns |    305.395 ns |   255.018 ns | 1,041.50 ns |  0.99 |    0.28 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |   546.08 ns |    136.807 ns |   114.240 ns |   541.00 ns |  0.49 |    0.13 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    36.81 ns |      8.682 ns |     7.250 ns |    34.50 ns |  0.03 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 1,146.23 ns |    216.048 ns |   180.410 ns | 1,142.00 ns |  1.03 |    0.23 |     440 B |        1.00 |
|                                     |            |                |             |             |             |               |              |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   993.33 ns | 14,881.224 ns |   815.690 ns | 1,267.00 ns |  0.70 |    0.66 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   263.67 ns |  4,715.935 ns |   258.496 ns |   190.00 ns |  0.19 |    0.20 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,270.00 ns | 16,452.187 ns |   901.800 ns | 3,079.00 ns |  2.31 |    1.32 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 2,786.33 ns |  8,745.013 ns |   479.344 ns | 2,940.00 ns |  1.97 |    1.05 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 2,029.67 ns | 19,863.505 ns | 1,088.786 ns | 1,563.00 ns |  1.43 |    1.03 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   207.00 ns |  3,867.368 ns |   211.983 ns |   111.00 ns |  0.15 |    0.16 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 1,822.67 ns | 20,146.897 ns | 1,104.319 ns | 1,562.00 ns |  1.29 |    0.99 |     440 B |        1.00 |
