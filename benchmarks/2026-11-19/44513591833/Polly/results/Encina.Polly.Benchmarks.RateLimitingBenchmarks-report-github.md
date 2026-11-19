```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.95GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   238.8 ns |     69.97 ns |    58.43 ns |   233.0 ns |  0.08 |    0.02 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   252.4 ns |     49.17 ns |    43.59 ns |   242.0 ns |  0.09 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,118.4 ns |    210.51 ns |   164.35 ns | 4,131.5 ns |  1.46 |    0.14 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,113.5 ns |    146.49 ns |   122.33 ns | 2,107.0 ns |  0.75 |    0.08 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 4,168.3 ns |    948.42 ns |   887.16 ns | 4,354.0 ns |  1.48 |    0.33 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   226.2 ns |     54.52 ns |    48.33 ns |   225.0 ns |  0.08 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,843.8 ns |    313.04 ns |   244.40 ns | 2,923.0 ns |  1.01 |    0.12 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   432.8 ns |  4,678.42 ns |   256.44 ns |   325.5 ns |  0.11 |    0.06 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   656.0 ns |  5,407.63 ns |   296.41 ns |   638.0 ns |  0.17 |    0.08 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 5,578.7 ns | 33,182.77 ns | 1,818.86 ns | 4,871.0 ns |  1.42 |    0.52 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 5,298.5 ns | 18,204.02 ns |   997.82 ns | 5,442.5 ns |  1.35 |    0.38 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 4,368.0 ns | 22,248.54 ns | 1,219.52 ns | 4,467.0 ns |  1.11 |    0.37 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   408.8 ns |  5,158.57 ns |   282.76 ns |   330.5 ns |  0.10 |    0.07 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 4,144.8 ns | 22,434.46 ns | 1,229.71 ns | 3,678.5 ns |  1.05 |    0.37 |     440 B |        1.00 |
