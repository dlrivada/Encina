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
| SingleHandler              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.278 μs | 0.0202 μs | 0.0133 μs |  1.00 |    0.00 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      6.631 μs | 0.0699 μs | 0.0462 μs |  1.55 |    0.01 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.303 μs | 0.0425 μs | 0.0281 μs |  1.24 |    0.01 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.944 μs | 0.0946 μs | 0.0563 μs |  1.86 |    0.01 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.178 μs | 0.0273 μs | 0.0181 μs |  1.21 |    0.01 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     25.950 μs | 0.2493 μs | 0.1484 μs |  6.07 |    0.04 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |             |              |             |               |           |           |       |         |        |        |           |             |
| SingleHandler              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 73,038.335 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 77,010.197 μs |        NA | 0.0000 μs |  1.05 |    0.00 |      - |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 72,982.651 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 77,808.544 μs |        NA | 0.0000 μs |  1.07 |    0.00 |      - |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 75,522.546 μs |        NA | 0.0000 μs |  1.03 |    0.00 |      - |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 74,123.553 μs |        NA | 0.0000 μs |  1.01 |    0.00 |      - |      - |  19.69 KB |        7.64 |
