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
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 103,019.0 ns |   948.57 ns | 627.42 ns | 96.41 |    0.58 | 2.1973 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,331.7 ns |     2.99 ns |   1.98 ns |  1.25 |    0.00 | 0.0267 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     211.3 ns |     1.18 ns |   0.70 ns |  0.20 |    0.00 | 0.0091 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  11,487.9 ns |    76.77 ns |  40.15 ns | 10.75 |    0.04 | 0.2289 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,068.5 ns |     2.94 ns |   1.94 ns |  1.00 |    0.00 | 0.0229 |     608 B |        1.00 |
|                                      |            |                |             |              |             |           |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 100,221.3 ns | 3,710.39 ns | 203.38 ns | 94.58 |    0.25 | 2.1973 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,289.8 ns |    28.83 ns |   1.58 ns |  1.22 |    0.00 | 0.0267 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     210.1 ns |     2.88 ns |   0.16 ns |  0.20 |    0.00 | 0.0091 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  11,335.3 ns |   254.62 ns |  13.96 ns | 10.70 |    0.02 | 0.2289 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,059.6 ns |    44.12 ns |   2.42 ns |  1.00 |    0.00 | 0.0229 |     608 B |        1.00 |
