```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error        | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|-------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 154,198.0 ns |    841.53 ns |   500.78 ns | 138.42 |    0.71 | 0.4883 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,778.3 ns |      7.37 ns |     4.39 ns |   1.60 |    0.01 | 0.0076 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     536.4 ns |      2.02 ns |     1.20 ns |   0.48 |    0.00 | 0.0019 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  15,941.7 ns |     86.84 ns |    57.44 ns |  14.31 |    0.08 | 0.0610 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,114.0 ns |      7.22 ns |     4.77 ns |   1.00 |    0.01 | 0.0057 |     608 B |        1.00 |
|                                      |            |                |             |              |              |             |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 155,085.1 ns | 18,491.74 ns | 1,013.59 ns | 139.88 |    0.89 | 0.4883 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,783.8 ns |    191.94 ns |    10.52 ns |   1.61 |    0.01 | 0.0076 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     545.7 ns |     70.24 ns |     3.85 ns |   0.49 |    0.00 | 0.0019 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  15,960.1 ns |     78.74 ns |     4.32 ns |  14.40 |    0.04 | 0.0610 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,108.7 ns |     69.14 ns |     3.79 ns |   1.00 |    0.00 | 0.0057 |     608 B |        1.00 |
