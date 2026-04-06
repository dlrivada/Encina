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
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,408.8 ns |  10.60 ns |   7.01 ns |   1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        350.6 ns |   0.69 ns |   0.41 ns |   0.25 |    0.00 | 0.0138 |     232 B |        0.38 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,890.9 ns |   2.37 ns |   1.41 ns |   1.34 |    0.01 | 0.0420 |     704 B |        1.16 |
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    145,836.0 ns | 530.35 ns | 350.79 ns | 103.52 |    0.55 | 3.4180 |   57600 B |       94.74 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     15,739.8 ns |  53.96 ns |  28.22 ns |  11.17 |    0.06 | 0.3357 |    5984 B |        9.84 |
|                                      |            |                |             |             |              |             |                 |           |           |        |         |        |           |             |
| TryAcquireAsync_SingleLock           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,904,170.0 ns |        NA |   0.00 ns |   1.00 |    0.00 |      - |     584 B |        1.00 |
| IsLockedAsync_UnlockedResource       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,301,175.0 ns |        NA |   0.00 ns |   0.39 |    0.00 |      - |     208 B |        0.36 |
| IsLockedAsync_LockedResource         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,565,938.0 ns |        NA |   0.00 ns |   0.97 |    0.00 |      - |     680 B |        1.16 |
| AcquireAndRelease_100Iterations      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,898,048.0 ns |        NA |   0.00 ns |   1.00 |    0.00 |      - |   56000 B |       95.89 |
| ParallelAcquire_10DifferentResources | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 14,689,010.0 ns |        NA |   0.00 ns |   1.35 |    0.00 |      - |    5744 B |        9.84 |
