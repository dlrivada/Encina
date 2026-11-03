```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.63GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 108,535.0 ns |   291.98 ns | 193.13 ns | 99.53 |    0.21 | 2.1973 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,353.3 ns |     2.24 ns |   1.33 ns |  1.24 |    0.00 | 0.0267 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     215.7 ns |     0.31 ns |   0.18 ns |  0.20 |    0.00 | 0.0091 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  11,567.5 ns |    41.29 ns |  27.31 ns | 10.61 |    0.03 | 0.2289 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,090.5 ns |     2.46 ns |   1.46 ns |  1.00 |    0.00 | 0.0229 |     608 B |        1.00 |
|                                      |            |                |             |              |             |           |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 101,622.5 ns | 3,626.59 ns | 198.79 ns | 93.22 |    0.41 | 2.1973 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,376.7 ns |   181.66 ns |   9.96 ns |  1.26 |    0.01 | 0.0267 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     208.1 ns |    14.13 ns |   0.77 ns |  0.19 |    0.00 | 0.0091 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  11,392.3 ns |   328.36 ns |  18.00 ns | 10.45 |    0.04 | 0.2289 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,090.2 ns |    92.68 ns |   5.08 ns |  1.00 |    0.01 | 0.0229 |     608 B |        1.00 |
