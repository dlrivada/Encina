```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 103,413.0 ns | 1,208.41 ns | 799.29 ns | 95.19 |    1.10 | 2.1973 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,419.0 ns |     4.18 ns |   2.77 ns |  1.31 |    0.01 | 0.0267 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     212.2 ns |     3.98 ns |   2.63 ns |  0.20 |    0.00 | 0.0091 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  11,797.5 ns |    29.62 ns |  17.63 ns | 10.86 |    0.10 | 0.2289 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,086.5 ns |    15.19 ns |  10.05 ns |  1.00 |    0.01 | 0.0229 |     608 B |        1.00 |
|                                      |            |                |             |              |             |           |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 104,805.3 ns | 9,844.07 ns | 539.59 ns | 93.03 |    0.45 | 2.1973 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,363.6 ns |   116.23 ns |   6.37 ns |  1.21 |    0.01 | 0.0267 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     220.5 ns |     9.53 ns |   0.52 ns |  0.20 |    0.00 | 0.0091 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  11,874.9 ns |   353.69 ns |  19.39 ns | 10.54 |    0.03 | 0.2289 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,126.6 ns |    47.64 ns |   2.61 ns |  1.00 |    0.00 | 0.0229 |     608 B |        1.00 |
