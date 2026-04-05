```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                              | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | Mean           | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |---------------- |--------------- |------------ |------------ |------------ |---------------:|----------:|----------:|------:|--------:|----------:|------------:|
| AcquireAsync_SimpleRateLimiting     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,315.9 ns | 661.65 ns | 171.83 ns |  1.00 |    0.07 |     440 B |        1.00 |
| AcquireAsync_WithAdaptiveThrottling | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     4,221.7 ns | 205.86 ns |  53.46 ns |  1.28 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       523.5 ns | 261.85 ns |  40.52 ns |  0.16 |    0.01 |         - |        0.00 |
| RecordFailure                       | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       518.2 ns | 145.97 ns |  22.59 ns |  0.16 |    0.01 |         - |        0.00 |
| GetState                            | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |       463.8 ns |  31.38 ns |   4.86 ns |  0.14 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,697.1 ns | 357.41 ns |  92.82 ns |  1.12 |    0.06 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     3,827.2 ns | 781.93 ns | 203.06 ns |  1.16 |    0.08 |     336 B |        0.76 |
|                                     |            |                 |                |             |             |             |                |           |           |       |         |           |             |
| AcquireAsync_SimpleRateLimiting     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,174,587.0 ns |        NA |   0.00 ns |  1.00 |    0.00 |     440 B |        1.00 |
| AcquireAsync_WithAdaptiveThrottling | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 7,188,503.0 ns |        NA |   0.00 ns |  1.00 |    0.00 |     440 B |        1.00 |
| RecordSuccess                       | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   319,305.0 ns |        NA |   0.00 ns |  0.04 |    0.00 |         - |        0.00 |
| RecordFailure                       | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   295,170.0 ns |        NA |   0.00 ns |  0.04 |    0.00 |         - |        0.00 |
| GetState                            | Dry        | Default         | 1              | 1           | ColdStart   | 1           |   452,103.0 ns |        NA |   0.00 ns |  0.06 |    0.00 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 6,915,675.0 ns |        NA |   0.00 ns |  0.96 |    0.00 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 6,974,524.0 ns |        NA |   0.00 ns |  0.97 |    0.00 |     336 B |        0.76 |
