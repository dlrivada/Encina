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
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,405.8 ns |  13.32 ns |   8.81 ns |   1.00 |    0.01 | 0.0362 |     608 B |        1.00 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        341.9 ns |   0.97 ns |   0.58 ns |   0.24 |    0.00 | 0.0138 |     232 B |        0.38 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,863.3 ns |  18.05 ns |  10.74 ns |   1.33 |    0.01 | 0.0420 |     704 B |        1.16 |
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    143,575.0 ns | 527.41 ns | 348.85 ns | 102.14 |    0.65 | 3.4180 |   57600 B |       94.74 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     15,707.2 ns |  46.72 ns |  30.90 ns |  11.17 |    0.07 | 0.3357 |    5984 B |        9.84 |
|                                      |            |                |             |             |              |             |                 |           |           |        |         |        |           |             |
| TryAcquireAsync_SingleLock           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,170,557.0 ns |        NA |   0.00 ns |   1.00 |    0.00 |      - |     584 B |        1.00 |
| IsLockedAsync_UnlockedResource       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,140,076.0 ns |        NA |   0.00 ns |   0.41 |    0.00 |      - |     208 B |        0.36 |
| IsLockedAsync_LockedResource         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,355,843.0 ns |        NA |   0.00 ns |   1.02 |    0.00 |      - |     680 B |        1.16 |
| AcquireAndRelease_100Iterations      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10,345,514.0 ns |        NA |   0.00 ns |   1.02 |    0.00 |      - |   56000 B |       95.89 |
| ParallelAcquire_10DifferentResources | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,229,382.0 ns |        NA |   0.00 ns |   1.30 |    0.00 |      - |    5744 B |        9.84 |
