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
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 147,708.6 ns |   500.96 ns | 331.35 ns | 139.99 |    0.42 | 0.4883 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,706.9 ns |     9.20 ns |   6.09 ns |   1.62 |    0.01 | 0.0076 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     515.4 ns |     1.49 ns |   0.88 ns |   0.49 |    0.00 | 0.0019 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  15,338.1 ns |    87.31 ns |  51.96 ns |  14.54 |    0.06 | 0.0610 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,055.1 ns |     3.51 ns |   2.32 ns |   1.00 |    0.00 | 0.0057 |     608 B |        1.00 |
|                                      |            |                |             |              |             |           |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 149,226.8 ns | 6,853.01 ns | 375.64 ns | 141.55 |    0.50 | 0.4883 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,758.0 ns |    10.85 ns |   0.59 ns |   1.67 |    0.00 | 0.0076 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     516.6 ns |    10.76 ns |   0.59 ns |   0.49 |    0.00 | 0.0019 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  15,721.3 ns | 1,011.69 ns |  55.45 ns |  14.91 |    0.06 | 0.0610 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,054.2 ns |    60.80 ns |   3.33 ns |   1.00 |    0.00 | 0.0057 |     608 B |        1.00 |
