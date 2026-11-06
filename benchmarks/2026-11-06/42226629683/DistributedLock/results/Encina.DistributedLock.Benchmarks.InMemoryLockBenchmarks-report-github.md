```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error        | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|-------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 148,982.3 ns |    566.84 ns |   296.47 ns | 101.29 |    0.21 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,898.1 ns |      3.26 ns |     1.71 ns |   1.29 |    0.00 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     344.2 ns |      0.93 ns |     0.55 ns |   0.23 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  16,259.0 ns |     14.87 ns |     8.85 ns |  11.05 |    0.01 | 0.3357 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,470.9 ns |      2.32 ns |     1.38 ns |   1.00 |    0.00 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |              |              |             |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 150,893.5 ns | 18,948.90 ns | 1,038.65 ns | 103.00 |    0.63 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,944.1 ns |    103.45 ns |     5.67 ns |   1.33 |    0.00 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     344.3 ns |     28.55 ns |     1.56 ns |   0.23 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  16,323.4 ns |    546.51 ns |    29.96 ns |  11.14 |    0.02 | 0.3357 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,465.0 ns |     46.97 ns |     2.57 ns |   1.00 |    0.00 | 0.0362 |     608 B |        1.00 |
