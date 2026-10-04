```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                               | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |-------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 3           | 148,567.4 ns | 395.91 ns | 235.60 ns | 100.90 |    0.98 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | 3           |   1,918.3 ns |   6.51 ns |   3.87 ns |   1.30 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | 3           |     351.5 ns |   1.41 ns |   0.93 ns |   0.24 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 3           |  16,097.7 ns |  44.59 ns |  26.53 ns |  10.93 |    0.11 | 0.3357 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | 3           |   1,472.6 ns |  22.38 ns |  14.80 ns |   1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |             |              |           |           |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | MediumRun  | 15             | 2           | 10          | 149,361.7 ns | 369.79 ns | 553.48 ns | 102.89 |    0.55 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | MediumRun  | 15             | 2           | 10          |   1,928.3 ns |   4.54 ns |   6.21 ns |   1.33 |    0.01 | 0.0420 |     704 B |        1.14 |
| IsLockedAsync_UnlockedResource       | MediumRun  | 15             | 2           | 10          |     344.6 ns |   0.56 ns |   0.84 ns |   0.24 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | MediumRun  | 15             | 2           | 10          |  16,121.3 ns |  71.66 ns | 100.45 ns |  11.11 |    0.08 | 0.3357 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | MediumRun  | 15             | 2           | 10          |   1,451.7 ns |   4.01 ns |   5.75 ns |   1.00 |    0.01 | 0.0362 |     616 B |        1.00 |
