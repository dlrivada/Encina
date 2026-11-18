```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 89,846.3 ns |   949.28 ns | 627.89 ns | 94.32 |    1.14 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |  1,081.1 ns |    17.12 ns |  11.32 ns |  1.13 |    0.02 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |    130.1 ns |     2.04 ns |   1.35 ns |  0.14 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  9,879.9 ns |    90.77 ns |  54.01 ns | 10.37 |    0.12 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |    952.7 ns |    15.22 ns |  10.07 ns |  1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |             |             |           |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 85,334.1 ns | 2,157.36 ns | 118.25 ns | 91.06 |    0.53 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |  1,082.4 ns |    60.73 ns |   3.33 ns |  1.16 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |    135.2 ns |    83.98 ns |   4.60 ns |  0.14 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           | 10,023.8 ns | 1,235.54 ns |  67.72 ns | 10.70 |    0.09 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |    937.2 ns |   111.77 ns |   6.13 ns |  1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
