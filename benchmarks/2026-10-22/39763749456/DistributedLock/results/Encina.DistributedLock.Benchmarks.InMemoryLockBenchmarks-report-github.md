```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error        | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|-------------:|----------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 104,474.2 ns |    872.42 ns | 577.05 ns | 93.99 |    0.69 | 2.1973 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,390.7 ns |      7.63 ns |   5.05 ns |  1.25 |    0.01 | 0.0267 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     214.3 ns |      0.84 ns |   0.44 ns |  0.19 |    0.00 | 0.0091 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  11,840.8 ns |     45.01 ns |  26.79 ns | 10.65 |    0.06 | 0.2289 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,111.6 ns |      8.86 ns |   5.86 ns |  1.00 |    0.01 | 0.0229 |     608 B |        1.00 |
|                                      |            |                |             |              |              |           |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 103,646.8 ns | 10,152.47 ns | 556.49 ns | 94.80 |    0.72 | 2.1973 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,348.2 ns |    282.85 ns |  15.50 ns |  1.23 |    0.01 | 0.0267 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     207.2 ns |      6.45 ns |   0.35 ns |  0.19 |    0.00 | 0.0091 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  11,415.0 ns |    281.42 ns |  15.43 ns | 10.44 |    0.06 | 0.2289 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,093.3 ns |    138.07 ns |   7.57 ns |  1.00 |    0.01 | 0.0229 |     608 B |        1.00 |
