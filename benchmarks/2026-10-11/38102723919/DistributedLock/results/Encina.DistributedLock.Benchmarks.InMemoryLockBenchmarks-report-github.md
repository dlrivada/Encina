```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                               | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 3           | 86,680.3 ns | 1,132.45 ns | 749.05 ns | 89.21 |    0.82 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | 3           |  1,059.6 ns |     5.50 ns |   3.64 ns |  1.09 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | 3           |    141.3 ns |    10.72 ns |   7.09 ns |  0.15 |    0.01 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 3           |  9,821.4 ns |   201.23 ns | 105.25 ns | 10.11 |    0.11 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | 3           |    971.7 ns |     7.74 ns |   4.05 ns |  1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |             |             |             |           |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | MediumRun  | 15             | 2           | 10          | 85,504.8 ns |   518.39 ns | 743.45 ns | 86.95 |    1.11 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | MediumRun  | 15             | 2           | 10          |  1,071.3 ns |    22.60 ns |  31.68 ns |  1.09 |    0.03 | 0.0420 |     720 B |        1.17 |
| IsLockedAsync_UnlockedResource       | MediumRun  | 15             | 2           | 10          |    140.1 ns |     5.58 ns |   8.36 ns |  0.14 |    0.01 | 0.0143 |     240 B |        0.39 |
| ParallelAcquire_10DifferentResources | MediumRun  | 15             | 2           | 10          | 10,138.5 ns |   148.70 ns | 213.26 ns | 10.31 |    0.23 | 0.3510 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | MediumRun  | 15             | 2           | 10          |    983.4 ns |     6.63 ns |   9.50 ns |  1.00 |    0.01 | 0.0362 |     616 B |        1.00 |
