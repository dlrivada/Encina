```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.33GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                               | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|------------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 3           | 95,445.9 ns |   592.00 ns |   391.57 ns | 94.70 |    0.80 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | 3           |  1,143.2 ns |     9.31 ns |     6.16 ns |  1.13 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | 3           |    138.1 ns |     6.46 ns |     3.84 ns |  0.14 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 3           | 10,679.0 ns |    66.31 ns |    43.86 ns | 10.60 |    0.09 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | 3           |  1,007.9 ns |    11.90 ns |     7.87 ns |  1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |             |             |             |             |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | MediumRun  | 15             | 2           | 10          | 90,787.8 ns | 1,244.66 ns | 1,862.95 ns | 89.75 |    2.48 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | MediumRun  | 15             | 2           | 10          |  1,152.9 ns |    12.10 ns |    18.10 ns |  1.14 |    0.03 | 0.0420 |     720 B |        1.17 |
| IsLockedAsync_UnlockedResource       | MediumRun  | 15             | 2           | 10          |    145.7 ns |     3.35 ns |     4.90 ns |  0.14 |    0.01 | 0.0143 |     240 B |        0.39 |
| ParallelAcquire_10DifferentResources | MediumRun  | 15             | 2           | 10          | 10,738.3 ns |    79.52 ns |   119.02 ns | 10.62 |    0.23 | 0.3510 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | MediumRun  | 15             | 2           | 10          |  1,012.0 ns |    13.26 ns |    19.84 ns |  1.00 |    0.03 | 0.0362 |     616 B |        1.00 |
