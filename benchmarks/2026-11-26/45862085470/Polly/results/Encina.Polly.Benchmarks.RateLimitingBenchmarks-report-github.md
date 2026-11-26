```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   509.0 ns |     56.62 ns |  50.19 ns |  0.14 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   458.7 ns |     60.00 ns |  50.11 ns |  0.12 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,829.1 ns |    361.05 ns | 320.06 ns |  1.02 |    0.13 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,952.4 ns |    260.02 ns | 243.22 ns |  1.05 |    0.12 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,757.2 ns |    347.56 ns | 308.10 ns |  1.00 |    0.13 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   545.2 ns |    102.22 ns |  90.61 ns |  0.15 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 3,786.1 ns |    427.82 ns | 400.18 ns |  1.01 |    0.14 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |           |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   421.3 ns |  1,898.60 ns | 104.07 ns |  0.12 |    0.03 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   454.3 ns |  1,381.39 ns |  75.72 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,564.7 ns |  2,932.27 ns | 160.73 ns |  1.31 |    0.05 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,739.0 ns | 16,206.38 ns | 888.33 ns |  1.36 |    0.22 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 4,522.0 ns | 18,221.19 ns | 998.76 ns |  1.30 |    0.25 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   484.3 ns |  1,369.29 ns |  75.06 ns |  0.14 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,487.0 ns |  1,558.74 ns |  85.44 ns |  1.00 |    0.03 |     440 B |        1.00 |
