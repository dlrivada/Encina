```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                     | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.265 μs | 0.0301 μs | 0.0179 μs |  1.00 |    0.01 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      6.677 μs | 0.0660 μs | 0.0436 μs |  1.57 |    0.01 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.252 μs | 0.0335 μs | 0.0199 μs |  1.23 |    0.01 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.819 μs | 0.0795 μs | 0.0526 μs |  1.83 |    0.01 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.125 μs | 0.0609 μs | 0.0403 μs |  1.20 |    0.01 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     25.246 μs | 0.1378 μs | 0.0911 μs |  5.92 |    0.03 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |             |              |             |               |           |           |       |         |        |        |           |             |
| SingleHandler              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 73,278.817 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 77,415.073 μs |        NA | 0.0000 μs |  1.06 |    0.00 |      - |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 73,875.726 μs |        NA | 0.0000 μs |  1.01 |    0.00 |      - |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 76,958.438 μs |        NA | 0.0000 μs |  1.05 |    0.00 |      - |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 76,223.727 μs |        NA | 0.0000 μs |  1.04 |    0.00 |      - |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 74,551.327 μs |        NA | 0.0000 μs |  1.02 |    0.00 |      - |      - |  19.69 KB |        7.64 |
