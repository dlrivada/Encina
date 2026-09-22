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
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   506.2 ns |     19.49 ns |    15.22 ns |  0.15 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   486.5 ns |     45.00 ns |    37.57 ns |  0.15 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,033.0 ns |    327.21 ns |   290.07 ns |  1.22 |    0.14 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,902.1 ns |    342.62 ns |   303.73 ns |  1.19 |    0.14 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,851.2 ns |    369.63 ns |   308.66 ns |  1.17 |    0.14 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   611.0 ns |    151.59 ns |   141.80 ns |  0.19 |    0.04 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,320.1 ns |    349.65 ns |   327.06 ns |  1.01 |    0.13 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   541.2 ns |  1,901.23 ns |   104.21 ns |  0.15 |    0.03 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   517.5 ns |  1,529.32 ns |    83.83 ns |  0.14 |    0.03 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,946.7 ns |  5,577.04 ns |   305.70 ns |  1.07 |    0.18 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 5,634.3 ns | 32,778.02 ns | 1,796.67 ns |  1.53 |    0.49 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,867.3 ns |  5,269.89 ns |   288.86 ns |  1.05 |    0.18 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   778.3 ns |  2,487.47 ns |   136.35 ns |  0.21 |    0.05 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,770.0 ns | 13,886.67 ns |   761.18 ns |  1.02 |    0.24 |     440 B |        1.00 |
