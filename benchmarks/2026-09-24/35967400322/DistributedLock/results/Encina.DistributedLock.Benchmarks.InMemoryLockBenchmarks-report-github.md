```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 88,698.3 ns | 1,109.14 ns | 733.63 ns | 94.40 |    1.81 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |  1,048.6 ns |     7.41 ns |   3.88 ns |  1.12 |    0.02 | 0.0420 |     704 B |        1.14 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |    137.8 ns |     3.62 ns |   2.15 ns |  0.15 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  9,763.5 ns |   105.45 ns |  69.75 ns | 10.39 |    0.19 | 0.3510 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |    939.8 ns |    25.62 ns |  16.95 ns |  1.00 |    0.02 | 0.0362 |     616 B |        1.00 |
|                                      |            |                |             |             |             |           |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 86,061.9 ns | 5,477.46 ns | 300.24 ns | 89.16 |    0.42 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |  1,066.6 ns |   266.57 ns |  14.61 ns |  1.10 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |    140.8 ns |   114.72 ns |   6.29 ns |  0.15 |    0.01 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  9,529.6 ns | 1,720.63 ns |  94.31 ns |  9.87 |    0.09 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |    965.3 ns |    72.23 ns |   3.96 ns |  1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
