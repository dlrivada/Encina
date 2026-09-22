```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 102,596.1 ns | 1,111.06 ns | 734.90 ns | 95.59 |    0.69 | 2.1973 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,315.4 ns |     5.20 ns |   3.44 ns |  1.23 |    0.00 | 0.0267 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     212.8 ns |     0.45 ns |   0.30 ns |  0.20 |    0.00 | 0.0091 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  11,529.4 ns |    71.07 ns |  47.01 ns | 10.74 |    0.05 | 0.2289 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,073.3 ns |     3.97 ns |   2.63 ns |  1.00 |    0.00 | 0.0229 |     608 B |        1.00 |
|                                      |            |                |             |              |             |           |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 101,347.0 ns | 2,820.65 ns | 154.61 ns | 94.56 |    0.99 | 2.1973 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,339.3 ns |    75.36 ns |   4.13 ns |  1.25 |    0.01 | 0.0267 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     212.9 ns |     5.01 ns |   0.27 ns |  0.20 |    0.00 | 0.0091 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  11,579.3 ns |   653.16 ns |  35.80 ns | 10.80 |    0.12 | 0.2289 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,071.9 ns |   235.96 ns |  12.93 ns |  1.00 |    0.01 | 0.0229 |     608 B |        1.00 |
