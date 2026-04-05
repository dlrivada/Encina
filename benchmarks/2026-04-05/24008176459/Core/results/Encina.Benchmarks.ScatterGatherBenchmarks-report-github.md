```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                     | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.348 μs | 0.0105 μs | 0.0069 μs |  1.00 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      6.743 μs | 0.0393 μs | 0.0260 μs |  1.55 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.478 μs | 0.0150 μs | 0.0089 μs |  1.26 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      8.241 μs | 0.0342 μs | 0.0204 μs |  1.90 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.206 μs | 0.0181 μs | 0.0108 μs |  1.20 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     25.822 μs | 0.0537 μs | 0.0355 μs |  5.94 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |             |              |             |               |           |           |       |        |        |           |             |
| SingleHandler              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 73,968.169 μs |        NA | 0.0000 μs |  1.00 |      - |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 78,571.055 μs |        NA | 0.0000 μs |  1.06 |      - |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 74,430.477 μs |        NA | 0.0000 μs |  1.01 |      - |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 77,136.961 μs |        NA | 0.0000 μs |  1.04 |      - |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 76,528.896 μs |        NA | 0.0000 μs |  1.03 |      - |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 75,196.162 μs |        NA | 0.0000 μs |  1.02 |      - |      - |  19.69 KB |        7.64 |
