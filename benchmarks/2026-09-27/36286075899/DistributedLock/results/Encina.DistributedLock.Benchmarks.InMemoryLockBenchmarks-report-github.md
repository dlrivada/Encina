```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                               | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error       | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |-------------:|------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 3           | 132,342.5 ns | 1,972.76 ns | 1,304.86 ns | 140.80 |    1.36 | 0.4883 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | 3           |   1,515.2 ns |    16.46 ns |     9.80 ns |   1.61 |    0.01 | 0.0076 |     704 B |        1.14 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | 3           |     464.4 ns |     4.02 ns |     2.66 ns |   0.49 |    0.00 | 0.0024 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 3           |  13,404.0 ns |    43.01 ns |    25.59 ns |  14.26 |    0.04 | 0.0610 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | 3           |     939.9 ns |     3.89 ns |     2.32 ns |   1.00 |    0.00 | 0.0067 |     616 B |        1.00 |
|                                      |            |                |             |             |              |             |             |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | MediumRun  | 15             | 2           | 10          | 131,711.1 ns |   781.95 ns | 1,170.39 ns | 141.06 |    1.27 | 0.4883 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | MediumRun  | 15             | 2           | 10          |   1,518.2 ns |     6.13 ns |     8.79 ns |   1.63 |    0.01 | 0.0076 |     720 B |        1.17 |
| IsLockedAsync_UnlockedResource       | MediumRun  | 15             | 2           | 10          |     455.1 ns |     2.47 ns |     3.54 ns |   0.49 |    0.00 | 0.0024 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | MediumRun  | 15             | 2           | 10          |  13,507.4 ns |    68.63 ns |    96.21 ns |  14.47 |    0.11 | 0.0610 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | MediumRun  | 15             | 2           | 10          |     933.7 ns |     1.35 ns |     2.01 ns |   1.00 |    0.00 | 0.0067 |     616 B |        1.00 |
