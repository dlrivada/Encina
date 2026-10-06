```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev       | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------:|--------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   242.67 ns |    265.542 ns |   207.318 ns |   188.00 ns |  0.10 |    0.08 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           | 1,814.93 ns |    975.476 ns |   912.461 ns | 1,959.00 ns |  0.74 |    0.37 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,292.43 ns |  1,126.209 ns |   998.355 ns | 4,489.00 ns |  1.75 |    0.43 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,329.36 ns |    660.944 ns |   585.910 ns | 4,465.50 ns |  1.76 |    0.29 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 1,870.92 ns |    106.885 ns |    89.254 ns | 1,840.00 ns |  0.76 |    0.08 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    77.25 ns |      9.726 ns |     7.593 ns |    75.00 ns |  0.03 |    0.00 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,480.38 ns |    304.105 ns |   253.942 ns | 2,457.00 ns |  1.01 |    0.14 |     440 B |        1.00 |
|                                     |            |                |             |             |             |               |              |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   414.50 ns |  4,171.235 ns |   228.639 ns |   374.50 ns |  0.12 |    0.07 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           | 1,389.33 ns | 20,222.032 ns | 1,108.438 ns | 1,556.00 ns |  0.39 |    0.32 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 5,175.50 ns | 18,221.242 ns |   998.768 ns | 5,304.50 ns |  1.45 |    0.62 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 5,465.67 ns | 25,567.009 ns | 1,401.414 ns | 5,505.00 ns |  1.53 |    0.70 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 5,304.83 ns | 42,412.811 ns | 2,324.789 ns | 4,675.50 ns |  1.48 |    0.83 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   617.83 ns |  6,712.508 ns |   367.935 ns |   510.50 ns |  0.17 |    0.12 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 4,115.83 ns | 33,696.310 ns | 1,847.008 ns | 3,844.50 ns |  1.15 |    0.65 |     440 B |        1.00 |
