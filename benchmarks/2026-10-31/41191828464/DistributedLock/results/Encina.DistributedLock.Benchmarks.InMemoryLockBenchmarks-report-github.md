```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error        | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|-------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 151,137.4 ns |    743.50 ns | 388.87 ns | 100.56 |    0.82 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,941.5 ns |      3.81 ns |   2.27 ns |   1.29 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     354.9 ns |      1.35 ns |   0.89 ns |   0.24 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  16,477.2 ns |     54.33 ns |  35.94 ns |  10.96 |    0.09 | 0.3357 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,503.1 ns |     18.53 ns |  12.26 ns |   1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |              |              |           |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 151,635.8 ns | 13,026.46 ns | 714.02 ns | 102.87 |    0.52 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,991.2 ns |    141.66 ns |   7.76 ns |   1.35 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     347.8 ns |     36.84 ns |   2.02 ns |   0.24 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  16,565.9 ns |    901.32 ns |  49.40 ns |  11.24 |    0.04 | 0.3357 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,474.1 ns |     92.14 ns |   5.05 ns |   1.00 |    0.00 | 0.0362 |     608 B |        1.00 |
