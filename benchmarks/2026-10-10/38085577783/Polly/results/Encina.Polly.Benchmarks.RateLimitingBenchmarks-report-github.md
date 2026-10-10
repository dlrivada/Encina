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
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |    50.92 ns |     33.856 ns |    28.271 ns |    50.00 ns |  0.08 |    0.04 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |    17.85 ns |      8.480 ns |     7.081 ns |    20.00 ns |  0.03 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,106.14 ns |    376.412 ns |   333.679 ns | 1,057.00 ns |  1.64 |    0.61 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           |   648.31 ns |    131.253 ns |   109.603 ns |   636.00 ns |  0.96 |    0.27 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           |   926.00 ns |    181.822 ns |   141.955 ns |   956.50 ns |  1.37 |    0.37 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |    20.85 ns |     18.002 ns |    15.032 ns |    20.00 ns |  0.03 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           |   707.08 ns |    176.039 ns |   147.000 ns |   716.00 ns |  1.05 |    0.32 |     440 B |        1.00 |
|                                     |            |                |             |             |             |               |              |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   230.33 ns |  3,841.607 ns |   210.571 ns |   140.00 ns |  0.09 |    0.08 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   236.50 ns |  3,881.971 ns |   212.784 ns |   139.50 ns |  0.09 |    0.08 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,051.00 ns | 18,060.539 ns |   989.959 ns | 2,664.00 ns |  1.22 |    0.46 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 1,823.00 ns | 22,291.806 ns | 1,221.889 ns | 1,422.00 ns |  0.73 |    0.47 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 1,867.67 ns | 19,172.966 ns | 1,050.935 ns | 1,467.00 ns |  0.75 |    0.41 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   578.33 ns | 16,505.492 ns |   904.722 ns |    61.00 ns |  0.23 |    0.33 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,666.00 ns | 15,973.825 ns |   875.579 ns | 2,259.00 ns |  1.07 |    0.40 |     440 B |        1.00 |
