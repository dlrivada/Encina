```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                               | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error     | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |-------------:|----------:|------------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 3           | 145,419.3 ns | 748.58 ns |   495.14 ns | 103.73 |    0.47 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | 3           |   1,860.2 ns |   9.13 ns |     6.04 ns |   1.33 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | 3           |     339.5 ns |   0.73 ns |     0.48 ns |   0.24 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 3           |  15,620.3 ns |  67.39 ns |    40.11 ns |  11.14 |    0.04 | 0.3357 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | 3           |   1,402.0 ns |   8.03 ns |     4.78 ns |   1.00 |    0.00 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |             |              |           |             |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | MediumRun  | 15             | 2           | 10          | 144,225.1 ns | 908.91 ns | 1,332.26 ns | 103.52 |    1.12 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | MediumRun  | 15             | 2           | 10          |   1,850.7 ns |   4.98 ns |     7.45 ns |   1.33 |    0.01 | 0.0420 |     720 B |        1.17 |
| IsLockedAsync_UnlockedResource       | MediumRun  | 15             | 2           | 10          |     338.9 ns |   1.54 ns |     2.21 ns |   0.24 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | MediumRun  | 15             | 2           | 10          |  15,711.0 ns | 123.90 ns |   177.70 ns |  11.28 |    0.14 | 0.3357 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | MediumRun  | 15             | 2           | 10          |   1,393.2 ns |   5.62 ns |     8.40 ns |   1.00 |    0.01 | 0.0362 |     616 B |        1.00 |
