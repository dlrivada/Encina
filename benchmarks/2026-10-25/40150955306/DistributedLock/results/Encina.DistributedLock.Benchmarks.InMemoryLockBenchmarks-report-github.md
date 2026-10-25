```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                               | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error     | StdDev    | Median       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |-------------:|----------:|----------:|-------------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 3           | 119,095.2 ns | 552.53 ns | 328.80 ns | 119,067.5 ns | 95.41 |    0.26 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | 3           |   1,409.0 ns |   3.38 ns |   1.77 ns |   1,409.4 ns |  1.13 |    0.00 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | 3           |     167.0 ns |   0.17 ns |   0.10 ns |     167.0 ns |  0.13 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 3           |  12,902.2 ns |  30.68 ns |  18.26 ns |  12,905.2 ns | 10.34 |    0.02 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | 3           |   1,248.3 ns |   1.82 ns |   1.08 ns |   1,248.1 ns |  1.00 |    0.00 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |             |              |           |           |              |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | MediumRun  | 15             | 2           | 10          | 117,626.4 ns | 108.08 ns | 151.51 ns | 117,627.3 ns | 93.77 |    0.77 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | MediumRun  | 15             | 2           | 10          |   1,420.4 ns |   3.64 ns |   5.33 ns |   1,419.6 ns |  1.13 |    0.01 | 0.0420 |     720 B |        1.17 |
| IsLockedAsync_UnlockedResource       | MediumRun  | 15             | 2           | 10          |     169.9 ns |   0.23 ns |   0.33 ns |     170.0 ns |  0.14 |    0.00 | 0.0143 |     240 B |        0.39 |
| ParallelAcquire_10DifferentResources | MediumRun  | 15             | 2           | 10          |  13,012.7 ns |  78.01 ns | 114.34 ns |  13,044.7 ns | 10.37 |    0.12 | 0.3510 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | MediumRun  | 15             | 2           | 10          |   1,254.5 ns |   7.02 ns |  10.29 ns |   1,261.4 ns |  1.00 |    0.01 | 0.0362 |     616 B |        1.00 |
