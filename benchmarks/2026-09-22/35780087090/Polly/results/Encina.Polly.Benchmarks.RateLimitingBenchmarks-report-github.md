```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   557.8 ns |     32.21 ns |    26.90 ns |  0.15 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   521.5 ns |     37.66 ns |    31.45 ns |  0.14 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,942.3 ns |    200.30 ns |   167.26 ns |  1.05 |    0.13 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,937.3 ns |    379.81 ns |   336.69 ns |  1.05 |    0.15 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,469.7 ns |    105.79 ns |    88.34 ns |  0.92 |    0.11 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   786.2 ns |    379.41 ns |   316.83 ns |  0.21 |    0.09 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,802.0 ns |    480.06 ns |   425.56 ns |  1.01 |    0.16 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   711.7 ns |  1,596.75 ns |    87.52 ns |  0.18 |    0.02 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   504.0 ns |  1,244.99 ns |    68.24 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 5,640.7 ns | 28,136.82 ns | 1,542.27 ns |  1.41 |    0.35 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,052.5 ns |  1,930.73 ns |   105.83 ns |  1.01 |    0.08 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,716.7 ns |  3,891.11 ns |   213.28 ns |  0.93 |    0.08 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   693.8 ns |  2,556.29 ns |   140.12 ns |  0.17 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 4,023.3 ns |  6,592.05 ns |   361.33 ns |  1.01 |    0.11 |     440 B |        1.00 |
