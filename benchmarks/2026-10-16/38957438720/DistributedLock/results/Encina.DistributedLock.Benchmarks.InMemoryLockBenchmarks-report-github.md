```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 86,635.3 ns | 1,369.56 ns | 716.30 ns | 95.85 |    1.62 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |  1,065.3 ns |    10.53 ns |   6.96 ns |  1.18 |    0.02 | 0.0420 |     704 B |        1.14 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |    140.2 ns |     9.09 ns |   5.41 ns |  0.16 |    0.01 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  9,677.7 ns |   175.75 ns | 104.58 ns | 10.71 |    0.19 | 0.3510 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |    904.1 ns |    21.57 ns |  14.26 ns |  1.00 |    0.02 | 0.0362 |     616 B |        1.00 |
|                                      |            |                |             |             |             |           |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 87,589.4 ns | 6,257.22 ns | 342.98 ns | 93.99 |    0.74 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |  1,160.3 ns |   691.66 ns |  37.91 ns |  1.25 |    0.04 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |    136.2 ns |   148.85 ns |   8.16 ns |  0.15 |    0.01 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  9,457.7 ns |   758.84 ns |  41.59 ns | 10.15 |    0.08 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |    932.0 ns |   138.94 ns |   7.62 ns |  1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
