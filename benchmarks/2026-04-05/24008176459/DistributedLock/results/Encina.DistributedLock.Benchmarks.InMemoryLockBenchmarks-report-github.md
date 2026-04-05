```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                               | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error       | StdDev      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|------------:|------------:|------:|--------:|-------:|----------:|------------:|
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,059.0 ns |     4.12 ns |     2.73 ns |  1.00 |    0.00 | 0.0229 |     608 B |        1.00 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        207.8 ns |     2.62 ns |     1.73 ns |  0.20 |    0.00 | 0.0091 |     232 B |        0.38 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,311.2 ns |     2.65 ns |     1.75 ns |  1.24 |    0.00 | 0.0267 |     704 B |        1.16 |
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    101,799.8 ns | 1,826.46 ns | 1,208.09 ns | 96.13 |    1.11 | 2.1973 |   57600 B |       94.74 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     11,517.2 ns |    65.19 ns |    43.12 ns | 10.88 |    0.05 | 0.2289 |    5984 B |        9.84 |
|                                      |            |                |             |             |              |             |                 |             |             |       |         |        |           |             |
| TryAcquireAsync_SingleLock           | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  9,499,180.0 ns |          NA |     0.00 ns |  1.00 |    0.00 |      - |     584 B |        1.00 |
| IsLockedAsync_UnlockedResource       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,889,011.0 ns |          NA |     0.00 ns |  0.41 |    0.00 |      - |     208 B |        0.36 |
| IsLockedAsync_LockedResource         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  9,801,471.0 ns |          NA |     0.00 ns |  1.03 |    0.00 |      - |     680 B |        1.16 |
| AcquireAndRelease_100Iterations      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  9,824,613.0 ns |          NA |     0.00 ns |  1.03 |    0.00 |      - |   56000 B |       95.89 |
| ParallelAcquire_10DifferentResources | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,347,307.0 ns |          NA |     0.00 ns |  1.30 |    0.00 |      - |    5744 B |        9.84 |
