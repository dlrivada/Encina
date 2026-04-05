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
| SingleHandler              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.153 μs | 0.0269 μs | 0.0178 μs |  1.00 |    0.01 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      6.660 μs | 0.0523 μs | 0.0311 μs |  1.60 |    0.01 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.290 μs | 0.0389 μs | 0.0258 μs |  1.27 |    0.01 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.820 μs | 0.0456 μs | 0.0302 μs |  1.88 |    0.01 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.175 μs | 0.0191 μs | 0.0126 μs |  1.25 |    0.01 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     26.023 μs | 0.4405 μs | 0.2913 μs |  6.27 |    0.07 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |             |              |             |               |           |           |       |         |        |        |           |             |
| SingleHandler              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 72,489.599 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 75,610.120 μs |        NA | 0.0000 μs |  1.04 |    0.00 |      - |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 72,262.130 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 75,330.581 μs |        NA | 0.0000 μs |  1.04 |    0.00 |      - |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 74,321.100 μs |        NA | 0.0000 μs |  1.03 |    0.00 |      - |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 72,296.073 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |  19.69 KB |        7.64 |
