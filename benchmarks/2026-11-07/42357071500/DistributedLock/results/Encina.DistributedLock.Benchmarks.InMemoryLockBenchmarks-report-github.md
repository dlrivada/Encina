```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 87,337.5 ns |   522.40 ns | 310.87 ns | 95.46 |    0.39 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |  1,064.7 ns |     9.04 ns |   5.98 ns |  1.16 |    0.01 | 0.0420 |     704 B |        1.14 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |    128.9 ns |     0.96 ns |   0.50 ns |  0.14 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  9,738.1 ns |   115.01 ns |  76.07 ns | 10.64 |    0.08 | 0.3510 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |    915.0 ns |     3.34 ns |   2.21 ns |  1.00 |    0.00 | 0.0362 |     616 B |        1.00 |
|                                      |            |                |             |             |             |           |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 85,782.8 ns | 4,166.72 ns | 228.39 ns | 93.46 |    0.79 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |  1,057.4 ns |    54.07 ns |   2.96 ns |  1.15 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |    133.9 ns |    35.43 ns |   1.94 ns |  0.15 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  9,633.0 ns |   725.05 ns |  39.74 ns | 10.50 |    0.09 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |    917.9 ns |   156.62 ns |   8.58 ns |  1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
