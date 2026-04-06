```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                               | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |-------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | 3           |   1,432.9 ns |   7.95 ns |   5.26 ns |   1.00 |    0.00 | 0.0362 |     608 B |        1.00 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | 3           |     350.5 ns |   1.10 ns |   0.72 ns |   0.24 |    0.00 | 0.0138 |     232 B |        0.38 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | 3           |   1,899.7 ns |   7.31 ns |   4.84 ns |   1.33 |    0.01 | 0.0420 |     704 B |        1.16 |
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 3           | 145,790.8 ns | 747.31 ns | 444.71 ns | 101.74 |    0.46 | 3.4180 |   57600 B |       94.74 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 3           |  15,860.8 ns |  43.20 ns |  25.71 ns |  11.07 |    0.04 | 0.3357 |    5984 B |        9.84 |
|                                      |            |                |             |             |              |           |           |        |         |        |           |             |
| TryAcquireAsync_SingleLock           | MediumRun  | 15             | 2           | 10          |   1,419.9 ns |   7.82 ns |  11.46 ns |   1.00 |    0.01 | 0.0362 |     616 B |        1.00 |
| IsLockedAsync_UnlockedResource       | MediumRun  | 15             | 2           | 10          |     346.3 ns |   1.46 ns |   2.09 ns |   0.24 |    0.00 | 0.0138 |     232 B |        0.38 |
| IsLockedAsync_LockedResource         | MediumRun  | 15             | 2           | 10          |   1,875.7 ns |   6.20 ns |   8.89 ns |   1.32 |    0.01 | 0.0420 |     720 B |        1.17 |
| AcquireAndRelease_100Iterations      | MediumRun  | 15             | 2           | 10          | 146,197.6 ns | 278.56 ns | 399.50 ns | 102.97 |    0.86 | 3.4180 |   57600 B |       93.51 |
| ParallelAcquire_10DifferentResources | MediumRun  | 15             | 2           | 10          |  15,757.5 ns |  61.66 ns |  86.43 ns |  11.10 |    0.11 | 0.3357 |    5984 B |        9.71 |
