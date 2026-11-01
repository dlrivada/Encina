```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                               | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error     | StdDev      | Median       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |-------------:|----------:|------------:|-------------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 3           | 123,366.7 ns | 300.59 ns |   178.87 ns | 123,374.7 ns | 96.39 |    0.19 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | 3           |   1,453.3 ns |   3.59 ns |     2.14 ns |   1,453.5 ns |  1.14 |    0.00 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | 3           |     171.8 ns |   1.73 ns |     1.14 ns |     171.4 ns |  0.13 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | 3           |  13,375.6 ns |  23.23 ns |    15.36 ns |  13,376.1 ns | 10.45 |    0.02 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | 3           |   1,279.9 ns |   2.86 ns |     1.89 ns |   1,280.5 ns |  1.00 |    0.00 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |             |              |           |             |              |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | MediumRun  | 15             | 2           | 10          | 122,885.6 ns | 747.22 ns | 1,022.81 ns | 123,305.8 ns | 95.19 |    0.83 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | MediumRun  | 15             | 2           | 10          |   1,467.5 ns |   1.97 ns |     2.69 ns |   1,467.4 ns |  1.14 |    0.00 | 0.0420 |     720 B |        1.17 |
| IsLockedAsync_UnlockedResource       | MediumRun  | 15             | 2           | 10          |     183.0 ns |   3.98 ns |     5.71 ns |     186.9 ns |  0.14 |    0.00 | 0.0143 |     240 B |        0.39 |
| ParallelAcquire_10DifferentResources | MediumRun  | 15             | 2           | 10          |  13,328.4 ns |  56.28 ns |    78.89 ns |  13,381.6 ns | 10.32 |    0.07 | 0.3510 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | MediumRun  | 15             | 2           | 10          |   1,291.0 ns |   2.76 ns |     3.95 ns |   1,290.8 ns |  1.00 |    0.00 | 0.0362 |     616 B |        1.00 |
