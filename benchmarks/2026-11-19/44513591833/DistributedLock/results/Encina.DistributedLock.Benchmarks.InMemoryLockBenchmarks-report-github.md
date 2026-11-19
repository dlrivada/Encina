```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 113,263.3 ns |   396.62 ns | 262.34 ns | 144.02 |    0.50 | 0.6104 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,356.5 ns |     2.78 ns |   1.65 ns |   1.72 |    0.01 | 0.0076 |     704 B |        1.14 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     412.3 ns |     0.78 ns |   0.41 ns |   0.52 |    0.00 | 0.0024 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  11,477.2 ns |    60.77 ns |  36.17 ns |  14.59 |    0.06 | 0.0610 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |     786.4 ns |     3.76 ns |   2.24 ns |   1.00 |    0.00 | 0.0067 |     616 B |        1.00 |
|                                      |            |                |             |              |             |           |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 111,418.7 ns | 8,713.45 ns | 477.61 ns | 137.62 |    0.94 | 0.6104 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,289.8 ns |   106.30 ns |   5.83 ns |   1.59 |    0.01 | 0.0076 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     413.0 ns |    46.84 ns |   2.57 ns |   0.51 |    0.00 | 0.0024 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  11,425.5 ns | 1,582.11 ns |  86.72 ns |  14.11 |    0.12 | 0.0610 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |     809.6 ns |    97.72 ns |   5.36 ns |   1.00 |    0.01 | 0.0067 |     608 B |        1.00 |
