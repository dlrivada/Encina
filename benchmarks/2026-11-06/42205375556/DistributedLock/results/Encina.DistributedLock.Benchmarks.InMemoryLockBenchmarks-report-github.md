```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error        | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|-------------:|----------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 125,347.6 ns |    378.60 ns | 198.01 ns | 94.72 |    0.46 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,479.0 ns |      7.80 ns |   4.64 ns |  1.12 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     170.8 ns |      1.38 ns |   0.92 ns |  0.13 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  13,574.3 ns |     24.42 ns |  14.53 ns | 10.26 |    0.05 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,323.3 ns |     12.33 ns |   6.45 ns |  1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |              |              |           |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 124,789.3 ns | 11,642.91 ns | 638.19 ns | 95.32 |    0.44 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,474.9 ns |     24.08 ns |   1.32 ns |  1.13 |    0.00 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     176.8 ns |      4.90 ns |   0.27 ns |  0.14 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  13,552.1 ns |    704.02 ns |  38.59 ns | 10.35 |    0.03 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,309.2 ns |     35.82 ns |   1.96 ns |  1.00 |    0.00 | 0.0362 |     608 B |        1.00 |
