```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------:|-------------:|----------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 90,758.2 ns |  1,009.35 ns | 600.65 ns | 89.32 |    3.49 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |  1,085.2 ns |     23.52 ns |  14.00 ns |  1.07 |    0.04 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |    142.0 ns |      2.39 ns |   1.25 ns |  0.14 |    0.01 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 10,094.8 ns |    134.72 ns |  70.46 ns |  9.93 |    0.39 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |  1,017.6 ns |     63.39 ns |  41.93 ns |  1.00 |    0.06 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |             |              |           |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 90,983.2 ns | 14,246.74 ns | 780.91 ns | 89.73 |    4.64 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |  1,109.7 ns |    111.24 ns |   6.10 ns |  1.09 |    0.06 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |    145.7 ns |     33.01 ns |   1.81 ns |  0.14 |    0.01 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  9,733.3 ns |    385.65 ns |  21.14 ns |  9.60 |    0.49 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |  1,016.4 ns |  1,130.20 ns |  61.95 ns |  1.00 |    0.07 | 0.0362 |     608 B |        1.00 |
