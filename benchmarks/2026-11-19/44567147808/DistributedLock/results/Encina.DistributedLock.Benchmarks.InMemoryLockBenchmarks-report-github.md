```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error        | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|-------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 113,595.5 ns |    535.83 ns | 354.42 ns | 139.79 |    0.44 | 0.6104 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,329.7 ns |      3.16 ns |   1.65 ns |   1.64 |    0.00 | 0.0076 |     704 B |        1.14 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     422.5 ns |      1.04 ns |   0.62 ns |   0.52 |    0.00 | 0.0024 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  11,929.7 ns |     75.52 ns |  39.50 ns |  14.68 |    0.05 | 0.0610 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |     812.6 ns |      1.62 ns |   0.96 ns |   1.00 |    0.00 | 0.0067 |     616 B |        1.00 |
|                                      |            |                |             |              |              |           |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 113,133.4 ns | 10,930.30 ns | 599.13 ns | 138.23 |    0.67 | 0.6104 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,345.0 ns |     83.35 ns |   4.57 ns |   1.64 |    0.01 | 0.0076 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     424.7 ns |     19.07 ns |   1.05 ns |   0.52 |    0.00 | 0.0024 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  12,250.1 ns |  2,519.55 ns | 138.11 ns |  14.97 |    0.15 | 0.0610 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |     818.4 ns |     27.75 ns |   1.52 ns |   1.00 |    0.00 | 0.0067 |     608 B |        1.00 |
