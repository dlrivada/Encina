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
| SingleHandler              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.182 μs | 0.0200 μs | 0.0119 μs |  1.00 |    0.00 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      6.722 μs | 0.0783 μs | 0.0466 μs |  1.61 |    0.01 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.389 μs | 0.0112 μs | 0.0058 μs |  1.29 |    0.00 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      8.078 μs | 0.0319 μs | 0.0211 μs |  1.93 |    0.01 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.255 μs | 0.0226 μs | 0.0149 μs |  1.26 |    0.00 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     25.824 μs | 0.1041 μs | 0.0689 μs |  6.18 |    0.02 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |             |              |             |               |           |           |       |         |        |        |           |             |
| SingleHandler              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 74,137.562 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 77,813.114 μs |        NA | 0.0000 μs |  1.05 |    0.00 |      - |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 74,769.188 μs |        NA | 0.0000 μs |  1.01 |    0.00 |      - |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 78,560.046 μs |        NA | 0.0000 μs |  1.06 |    0.00 |      - |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 76,646.853 μs |        NA | 0.0000 μs |  1.03 |    0.00 |      - |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 75,281.021 μs |        NA | 0.0000 μs |  1.02 |    0.00 |      - |      - |  19.69 KB |        7.64 |
