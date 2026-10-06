```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error        | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|-------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 112,982.3 ns |  6,483.01 ns | 4,288.11 ns | 146.18 |    5.31 | 0.6104 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,324.8 ns |     63.91 ns |    42.27 ns |   1.71 |    0.05 | 0.0076 |     704 B |        1.14 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     397.3 ns |     17.79 ns |    11.77 ns |   0.51 |    0.01 | 0.0024 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  11,157.2 ns |     85.87 ns |    44.91 ns |  14.44 |    0.07 | 0.0610 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |     772.9 ns |      3.82 ns |     2.00 ns |   1.00 |    0.00 | 0.0067 |     616 B |        1.00 |
|                                      |            |                |             |              |              |             |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 110,077.6 ns | 52,254.88 ns | 2,864.27 ns | 142.10 |    3.21 | 0.6104 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,308.4 ns |  1,004.77 ns |    55.07 ns |   1.69 |    0.06 | 0.0076 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     406.1 ns |    218.39 ns |    11.97 ns |   0.52 |    0.01 | 0.0024 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  11,281.8 ns |  6,291.70 ns |   344.87 ns |  14.56 |    0.39 | 0.0610 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |     774.7 ns |     16.21 ns |     0.89 ns |   1.00 |    0.00 | 0.0067 |     608 B |        1.00 |
