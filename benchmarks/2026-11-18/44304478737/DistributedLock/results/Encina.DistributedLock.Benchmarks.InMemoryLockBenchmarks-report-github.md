```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error       | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 154,487.4 ns | 2,236.60 ns | 1,479.37 ns | 143.20 |    1.47 | 0.4883 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,783.6 ns |     3.14 ns |     1.87 ns |   1.65 |    0.01 | 0.0076 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     540.8 ns |     0.92 ns |     0.55 ns |   0.50 |    0.00 | 0.0019 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  15,859.4 ns |   168.29 ns |   111.31 ns |  14.70 |    0.12 | 0.0610 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,078.8 ns |     8.04 ns |     5.32 ns |   1.00 |    0.01 | 0.0057 |     608 B |        1.00 |
|                                      |            |                |             |              |             |             |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 153,514.1 ns | 8,492.44 ns |   465.50 ns | 141.76 |    0.38 | 0.4883 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,760.1 ns |   236.22 ns |    12.95 ns |   1.63 |    0.01 | 0.0076 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     538.0 ns |    22.54 ns |     1.24 ns |   0.50 |    0.00 | 0.0019 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  15,757.4 ns | 2,321.16 ns |   127.23 ns |  14.55 |    0.10 | 0.0610 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,082.9 ns |     9.25 ns |     0.51 ns |   1.00 |    0.00 | 0.0057 |     608 B |        1.00 |
