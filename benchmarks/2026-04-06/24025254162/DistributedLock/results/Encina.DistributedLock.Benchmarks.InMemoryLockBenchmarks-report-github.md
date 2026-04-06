```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,062.6 ns |     4.07 ns |   2.69 ns |  1.00 |    0.00 | 0.0229 |     608 B |        1.00 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     212.0 ns |     4.01 ns |   2.65 ns |  0.20 |    0.00 | 0.0091 |     232 B |        0.38 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,319.5 ns |     3.11 ns |   2.06 ns |  1.24 |    0.00 | 0.0267 |     704 B |        1.16 |
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 100,554.6 ns |   455.18 ns | 301.07 ns | 94.63 |    0.35 | 2.1973 |   57600 B |       94.74 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  11,420.1 ns |    44.28 ns |  29.29 ns | 10.75 |    0.04 | 0.2289 |    5984 B |        9.84 |
|                                      |            |                |             |              |             |           |       |         |        |           |             |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,066.3 ns |    45.52 ns |   2.50 ns |  1.00 |    0.00 | 0.0229 |     608 B |        1.00 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     208.5 ns |    12.86 ns |   0.71 ns |  0.20 |    0.00 | 0.0091 |     232 B |        0.38 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,298.1 ns |   120.30 ns |   6.59 ns |  1.22 |    0.01 | 0.0267 |     704 B |        1.16 |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 100,773.6 ns | 5,329.78 ns | 292.14 ns | 94.50 |    0.31 | 2.1973 |   57600 B |       94.74 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  11,386.8 ns | 3,062.47 ns | 167.86 ns | 10.68 |    0.14 | 0.2289 |    5984 B |        9.84 |
