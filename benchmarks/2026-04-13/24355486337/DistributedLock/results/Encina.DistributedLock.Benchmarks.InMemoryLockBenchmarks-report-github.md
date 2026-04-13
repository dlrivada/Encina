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
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 3           | 151,085.8 ns | 700.50 ns | 416.86 ns | 150,981.5 ns | 102.84 |    0.91 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | 3           |   1,914.3 ns |   5.39 ns |   3.21 ns |   1,915.3 ns |   1.30 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | 3           |     353.7 ns |   1.19 ns |   0.79 ns |     353.5 ns |   0.24 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 3           |  15,947.0 ns |  42.32 ns |  22.13 ns |  15,947.8 ns |  10.85 |    0.09 | 0.3357 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | 3           |   1,469.2 ns |  19.76 ns |  13.07 ns |   1,465.5 ns |   1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |             |              |           |           |              |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | MediumRun  | 15             | 2           | 10          | 147,754.7 ns | 386.44 ns | 541.73 ns | 147,629.1 ns | 102.68 |    0.62 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | MediumRun  | 15             | 2           | 10          |   1,904.8 ns |   3.40 ns |   5.09 ns |   1,904.5 ns |   1.32 |    0.01 | 0.0420 |     704 B |        1.14 |
| IsLockedAsync_UnlockedResource       | MediumRun  | 15             | 2           | 10          |     347.3 ns |   0.53 ns |   0.75 ns |     347.2 ns |   0.24 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | MediumRun  | 15             | 2           | 10          |  16,458.7 ns | 132.28 ns | 189.72 ns |  16,357.9 ns |  11.44 |    0.14 | 0.3357 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | MediumRun  | 15             | 2           | 10          |   1,439.0 ns |   4.92 ns |   7.05 ns |   1,437.1 ns |   1.00 |    0.01 | 0.0362 |     616 B |        1.00 |
