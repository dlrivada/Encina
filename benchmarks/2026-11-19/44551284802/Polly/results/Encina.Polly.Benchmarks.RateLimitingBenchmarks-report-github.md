```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   395.8 ns |    128.87 ns | 107.61 ns |   345.0 ns |  0.13 |    0.03 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   375.8 ns |     71.36 ns |  66.75 ns |   369.5 ns |  0.12 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,427.7 ns |     96.18 ns |  80.32 ns | 3,429.5 ns |  1.10 |    0.06 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,742.1 ns |    270.10 ns | 239.44 ns | 3,645.5 ns |  1.20 |    0.09 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,222.0 ns |    119.88 ns | 100.10 ns | 3,209.5 ns |  1.03 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   526.5 ns |     27.58 ns |  24.45 ns |   530.0 ns |  0.17 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,133.8 ns |    203.01 ns | 158.50 ns | 3,190.0 ns |  1.00 |    0.07 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   486.0 ns |  1,315.57 ns |  72.11 ns |   466.0 ns |  0.13 |    0.03 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   528.0 ns |  4,172.07 ns | 228.69 ns |   401.0 ns |  0.14 |    0.06 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,259.0 ns | 10,043.45 ns | 550.52 ns | 4,536.0 ns |  1.13 |    0.24 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,220.0 ns |  6,139.68 ns | 336.54 ns | 4,057.0 ns |  1.12 |    0.22 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,706.0 ns |  9,708.01 ns | 532.13 ns | 3,666.0 ns |  0.98 |    0.22 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   691.0 ns |  4,457.60 ns | 244.34 ns |   581.0 ns |  0.18 |    0.07 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,899.2 ns | 15,774.99 ns | 864.68 ns | 3,725.5 ns |  1.03 |    0.28 |     440 B |        1.00 |
