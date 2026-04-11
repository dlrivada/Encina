```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                               | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error     | StdDev    | Median       | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |-------------:|----------:|----------:|-------------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 3           | 145,163.6 ns | 376.08 ns | 223.80 ns | 145,163.5 ns | 101.90 |    0.26 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | 3           |   1,882.0 ns |   2.67 ns |   1.59 ns |   1,881.7 ns |   1.32 |    0.00 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | 3           |     357.2 ns |   4.87 ns |   2.90 ns |     356.3 ns |   0.25 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 3           |  15,635.0 ns |  62.27 ns |  41.19 ns |  15,636.6 ns |  10.98 |    0.04 | 0.3357 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | 3           |   1,424.6 ns |   4.58 ns |   3.03 ns |   1,425.5 ns |   1.00 |    0.00 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |             |              |           |           |              |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | MediumRun  | 15             | 2           | 10          | 144,614.9 ns | 412.42 ns | 604.53 ns | 144,502.6 ns | 103.02 |    0.50 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | MediumRun  | 15             | 2           | 10          |   1,878.7 ns |   7.89 ns |  10.80 ns |   1,884.8 ns |   1.34 |    0.01 | 0.0420 |     720 B |        1.17 |
| IsLockedAsync_UnlockedResource       | MediumRun  | 15             | 2           | 10          |     344.6 ns |   1.01 ns |   1.45 ns |     344.9 ns |   0.25 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | MediumRun  | 15             | 2           | 10          |  15,800.9 ns |  48.08 ns |  67.40 ns |  15,815.4 ns |  11.26 |    0.06 | 0.3357 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | MediumRun  | 15             | 2           | 10          |   1,403.7 ns |   2.59 ns |   3.71 ns |   1,403.3 ns |   1.00 |    0.00 | 0.0362 |     616 B |        1.00 |
