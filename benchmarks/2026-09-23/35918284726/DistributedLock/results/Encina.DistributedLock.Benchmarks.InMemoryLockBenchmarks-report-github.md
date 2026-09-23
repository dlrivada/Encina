```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 154,630.5 ns |   685.67 ns | 453.53 ns | 138.47 |    0.43 | 0.4883 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,785.9 ns |    11.42 ns |   7.55 ns |   1.60 |    0.01 | 0.0076 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     542.2 ns |     4.27 ns |   2.83 ns |   0.49 |    0.00 | 0.0019 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  15,958.2 ns |   106.03 ns |  70.13 ns |  14.29 |    0.06 | 0.0610 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,116.7 ns |     2.50 ns |   1.49 ns |   1.00 |    0.00 | 0.0057 |     608 B |        1.00 |
|                                      |            |                |             |              |             |           |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 154,090.3 ns | 7,571.30 ns | 415.01 ns | 135.39 |    1.23 | 0.4883 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,799.6 ns |   272.53 ns |  14.94 ns |   1.58 |    0.02 | 0.0076 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     536.7 ns |    23.40 ns |   1.28 ns |   0.47 |    0.00 | 0.0019 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  15,978.5 ns | 2,993.11 ns | 164.06 ns |  14.04 |    0.18 | 0.0610 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,138.2 ns |   209.72 ns |  11.50 ns |   1.00 |    0.01 | 0.0057 |     608 B |        1.00 |
