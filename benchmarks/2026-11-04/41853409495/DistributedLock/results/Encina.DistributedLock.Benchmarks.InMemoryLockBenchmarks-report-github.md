```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------:|-------------:|----------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 95,116.1 ns |  1,416.39 ns | 842.87 ns | 96.17 |    0.96 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |  1,151.6 ns |     28.55 ns |  14.93 ns |  1.16 |    0.02 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |    138.8 ns |      2.71 ns |   1.61 ns |  0.14 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 10,628.5 ns |    389.38 ns | 257.55 ns | 10.75 |    0.26 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |    989.1 ns |      9.50 ns |   5.66 ns |  1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |             |              |           |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 92,644.2 ns | 11,815.76 ns | 647.66 ns | 95.08 |    0.98 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |  1,158.4 ns |    205.81 ns |  11.28 ns |  1.19 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |    147.0 ns |    127.92 ns |   7.01 ns |  0.15 |    0.01 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           | 10,406.0 ns |  1,083.31 ns |  59.38 ns | 10.68 |    0.10 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |    974.5 ns |    170.71 ns |   9.36 ns |  1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
