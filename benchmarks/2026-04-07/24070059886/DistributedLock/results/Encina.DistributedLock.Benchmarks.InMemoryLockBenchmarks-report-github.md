```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                               | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,382.1 ns |  10.00 ns |   5.95 ns |   1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        341.5 ns |   1.72 ns |   1.14 ns |   0.25 |    0.00 | 0.0138 |     232 B |        0.38 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,867.2 ns |   6.79 ns |   4.49 ns |   1.35 |    0.01 | 0.0420 |     704 B |        1.16 |
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    143,599.4 ns | 757.85 ns | 450.99 ns | 103.90 |    0.53 | 3.4180 |   57600 B |       94.74 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     15,561.1 ns |  73.62 ns |  48.70 ns |  11.26 |    0.06 | 0.3357 |    5984 B |        9.84 |
|                                      |            |                |             |             |              |             |                 |           |           |        |         |        |           |             |
| TryAcquireAsync_SingleLock           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,105,385.0 ns |        NA |   0.00 ns |   1.00 |    0.00 |      - |     584 B |        1.00 |
| IsLockedAsync_UnlockedResource       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,104,542.0 ns |        NA |   0.00 ns |   0.41 |    0.00 |      - |     208 B |        0.36 |
| IsLockedAsync_LockedResource         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,275,932.0 ns |        NA |   0.00 ns |   1.02 |    0.00 |      - |     680 B |        1.16 |
| AcquireAndRelease_100Iterations      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,272,395.0 ns |        NA |   0.00 ns |   1.02 |    0.00 |      - |   56000 B |       95.89 |
| ParallelAcquire_10DifferentResources | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,062,135.0 ns |        NA |   0.00 ns |   1.29 |    0.00 |      - |    5744 B |        9.84 |
