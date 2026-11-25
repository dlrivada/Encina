```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------:|-------------:|------------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 90,203.4 ns |  2,164.37 ns | 1,287.98 ns | 91.42 |    2.33 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |  1,122.8 ns |     19.21 ns |    12.71 ns |  1.14 |    0.03 | 0.0420 |     704 B |        1.14 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |    142.3 ns |     11.65 ns |     7.71 ns |  0.14 |    0.01 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 10,005.1 ns |    180.04 ns |   119.08 ns | 10.14 |    0.25 | 0.3510 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |    987.2 ns |     33.58 ns |    22.21 ns |  1.00 |    0.03 | 0.0362 |     616 B |        1.00 |
|                                      |            |                |             |             |              |             |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 89,202.1 ns | 14,281.64 ns |   782.82 ns | 91.19 |    1.04 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |  1,098.6 ns |     82.42 ns |     4.52 ns |  1.12 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |    145.0 ns |    123.78 ns |     6.78 ns |  0.15 |    0.01 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           | 10,018.8 ns |  1,385.66 ns |    75.95 ns | 10.24 |    0.11 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |    978.3 ns |    175.99 ns |     9.65 ns |  1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
