```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   361.7 ns |    89.67 ns |  79.49 ns |   345.0 ns |  0.14 |    0.03 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   280.0 ns |    28.49 ns |  23.79 ns |   281.0 ns |  0.11 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,515.4 ns |    72.07 ns |  56.27 ns | 2,528.0 ns |  0.98 |    0.07 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,753.2 ns |   295.44 ns | 261.90 ns | 2,649.0 ns |  1.07 |    0.13 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 2,542.0 ns |    92.21 ns |  81.74 ns | 2,544.0 ns |  0.99 |    0.08 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   242.4 ns |    91.50 ns |  71.44 ns |   220.0 ns |  0.09 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,588.8 ns |   214.83 ns | 200.95 ns | 2,514.0 ns |  1.01 |    0.10 |     440 B |        1.00 |
|                                     |            |                |             |             |            |             |           |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   233.5 ns | 1,485.16 ns |  81.41 ns |   280.5 ns |  0.09 |    0.03 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   541.3 ns | 3,173.06 ns | 173.93 ns |   581.0 ns |  0.21 |    0.06 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 2,874.3 ns | 5,805.13 ns | 318.20 ns | 2,794.0 ns |  1.12 |    0.15 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,304.8 ns | 2,518.05 ns | 138.02 ns | 3,274.5 ns |  1.29 |    0.13 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 2,724.3 ns | 4,011.56 ns | 219.89 ns | 2,634.0 ns |  1.06 |    0.13 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   429.7 ns | 1,325.70 ns |  72.67 ns |   450.0 ns |  0.17 |    0.03 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 2,591.0 ns | 5,071.85 ns | 278.01 ns | 2,634.0 ns |  1.01 |    0.13 |     440 B |        1.00 |
