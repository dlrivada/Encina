```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                               | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |-------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | 3           |   1,061.1 ns |   1.43 ns |   0.85 ns |  1.00 |    0.00 | 0.0229 |     608 B |        1.00 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | 3           |     213.0 ns |   2.46 ns |   1.63 ns |  0.20 |    0.00 | 0.0091 |     232 B |        0.38 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | 3           |   1,318.4 ns |   6.89 ns |   4.10 ns |  1.24 |    0.00 | 0.0267 |     704 B |        1.16 |
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 3           | 102,270.5 ns | 963.74 ns | 504.05 ns | 96.38 |    0.45 | 2.1973 |   57600 B |       94.74 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 3           |  11,423.2 ns |  62.31 ns |  41.21 ns | 10.77 |    0.04 | 0.2289 |    5984 B |        9.84 |
|                                      |            |                |             |             |              |           |           |       |         |        |           |             |
| TryAcquireAsync_SingleLock           | MediumRun  | 15             | 2           | 10          |   1,094.4 ns |   6.55 ns |   9.39 ns |  1.00 |    0.01 | 0.0229 |     616 B |        1.00 |
| IsLockedAsync_UnlockedResource       | MediumRun  | 15             | 2           | 10          |     213.2 ns |   2.04 ns |   3.05 ns |  0.19 |    0.00 | 0.0095 |     240 B |        0.39 |
| IsLockedAsync_LockedResource         | MediumRun  | 15             | 2           | 10          |   1,320.7 ns |  14.59 ns |  21.84 ns |  1.21 |    0.02 | 0.0286 |     720 B |        1.17 |
| AcquireAndRelease_100Iterations      | MediumRun  | 15             | 2           | 10          | 102,006.3 ns | 381.91 ns | 547.73 ns | 93.21 |    0.93 | 2.1973 |   57600 B |       93.51 |
| ParallelAcquire_10DifferentResources | MediumRun  | 15             | 2           | 10          |  11,383.4 ns |  38.95 ns |  54.61 ns | 10.40 |    0.10 | 0.2289 |    5984 B |        9.71 |
