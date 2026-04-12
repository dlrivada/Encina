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
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 3           | 146,653.5 ns | 562.08 ns | 334.48 ns | 102.84 |    0.37 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | 3           |   1,860.9 ns |   3.16 ns |   1.88 ns |   1.30 |    0.00 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | 3           |     340.8 ns |   1.03 ns |   0.68 ns |   0.24 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 3           |  15,464.9 ns |  50.04 ns |  33.10 ns |  10.84 |    0.04 | 0.3357 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | 3           |   1,426.1 ns |   6.53 ns |   4.32 ns |   1.00 |    0.00 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |             |              |           |           |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | MediumRun  | 15             | 2           | 10          | 142,911.2 ns | 434.45 ns | 623.08 ns | 103.50 |    0.64 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | MediumRun  | 15             | 2           | 10          |   1,855.1 ns |   2.31 ns |   3.39 ns |   1.34 |    0.01 | 0.0420 |     720 B |        1.17 |
| IsLockedAsync_UnlockedResource       | MediumRun  | 15             | 2           | 10          |     340.1 ns |   0.58 ns |   0.85 ns |   0.25 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | MediumRun  | 15             | 2           | 10          |  15,847.1 ns | 197.50 ns | 283.25 ns |  11.48 |    0.21 | 0.3357 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | MediumRun  | 15             | 2           | 10          |   1,380.8 ns |   4.34 ns |   6.22 ns |   1.00 |    0.01 | 0.0362 |     616 B |        1.00 |
