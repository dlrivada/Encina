```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                               | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 3           | 151,190.4 ns | 1,421.06 ns | 939.94 ns | 101.51 |    0.76 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | 3           |   1,919.3 ns |     6.88 ns |   4.09 ns |   1.29 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | 3           |     341.4 ns |     1.76 ns |   1.17 ns |   0.23 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 3           |  16,349.9 ns |    82.54 ns |  54.60 ns |  10.98 |    0.06 | 0.3357 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | 3           |   1,489.4 ns |    10.78 ns |   7.13 ns |   1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |             |              |             |           |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | MediumRun  | 15             | 2           | 10          | 150,472.5 ns |   445.65 ns | 653.23 ns | 102.05 |    0.53 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | MediumRun  | 15             | 2           | 10          |   1,938.1 ns |     5.99 ns |   8.97 ns |   1.31 |    0.01 | 0.0420 |     704 B |        1.14 |
| IsLockedAsync_UnlockedResource       | MediumRun  | 15             | 2           | 10          |     357.6 ns |     6.46 ns |   9.67 ns |   0.24 |    0.01 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | MediumRun  | 15             | 2           | 10          |  16,293.3 ns |    33.09 ns |  47.45 ns |  11.05 |    0.05 | 0.3357 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | MediumRun  | 15             | 2           | 10          |   1,474.5 ns |     3.02 ns |   4.43 ns |   1.00 |    0.00 | 0.0362 |     616 B |        1.00 |
