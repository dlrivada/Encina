```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                               | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,411.6 ns |   9.22 ns |   6.10 ns |   1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        346.6 ns |   1.47 ns |   0.97 ns |   0.25 |    0.00 | 0.0138 |     232 B |        0.38 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,873.6 ns |  10.84 ns |   7.17 ns |   1.33 |    0.01 | 0.0420 |     704 B |        1.16 |
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    143,705.4 ns | 532.06 ns | 316.62 ns | 101.80 |    0.47 | 3.4180 |   57600 B |       94.74 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     15,700.1 ns |  48.64 ns |  32.17 ns |  11.12 |    0.05 | 0.3357 |    5984 B |        9.84 |
|                                      |            |                |             |             |              |             |                 |           |           |        |         |        |           |             |
| TryAcquireAsync_SingleLock           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,294,811.0 ns |        NA |   0.00 ns |   1.00 |    0.00 |      - |     584 B |        1.00 |
| IsLockedAsync_UnlockedResource       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,168,993.0 ns |        NA |   0.00 ns |   0.40 |    0.00 |      - |     208 B |        0.36 |
| IsLockedAsync_LockedResource         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,363,061.0 ns |        NA |   0.00 ns |   1.01 |    0.00 |      - |     680 B |        1.16 |
| AcquireAndRelease_100Iterations      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,383,669.0 ns |        NA |   0.00 ns |   1.01 |    0.00 |      - |   56000 B |       95.89 |
| ParallelAcquire_10DifferentResources | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,062,077.0 ns |        NA |   0.00 ns |   1.27 |    0.00 |      - |    5744 B |        9.84 |
