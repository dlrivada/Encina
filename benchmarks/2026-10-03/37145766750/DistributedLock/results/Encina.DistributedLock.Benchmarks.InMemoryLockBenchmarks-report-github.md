```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 151,878.7 ns |   696.23 ns | 460.51 ns | 94.70 |    0.38 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,827.8 ns |     7.27 ns |   4.32 ns |  1.14 |    0.00 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     220.0 ns |     1.12 ns |   0.74 ns |  0.14 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  16,789.2 ns |    61.27 ns |  36.46 ns | 10.47 |    0.04 | 0.3357 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,603.7 ns |     7.91 ns |   4.71 ns |  1.00 |    0.00 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |              |             |           |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 153,114.3 ns | 8,010.19 ns | 439.07 ns | 94.31 |    0.72 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,869.3 ns |    84.44 ns |   4.63 ns |  1.15 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     225.2 ns |     4.49 ns |   0.25 ns |  0.14 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  16,720.4 ns |   526.64 ns |  28.87 ns | 10.30 |    0.08 | 0.3357 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,623.6 ns |   248.23 ns |  13.61 ns |  1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
