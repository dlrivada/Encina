```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  4.415 μs | 0.0345 μs | 0.0205 μs |  1.00 |    0.01 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  6.894 μs | 0.1040 μs | 0.0688 μs |  1.56 |    0.02 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  5.375 μs | 0.0519 μs | 0.0309 μs |  1.22 |    0.01 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  8.051 μs | 0.0965 μs | 0.0639 μs |  1.82 |    0.02 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  5.240 μs | 0.0169 μs | 0.0101 μs |  1.19 |    0.01 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 25.395 μs | 0.2451 μs | 0.1621 μs |  5.75 |    0.04 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |         |        |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  4.264 μs | 0.1456 μs | 0.0080 μs |  1.00 |    0.00 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  6.835 μs | 1.9233 μs | 0.1054 μs |  1.60 |    0.02 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  5.389 μs | 0.1978 μs | 0.0108 μs |  1.26 |    0.00 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  7.824 μs | 0.4199 μs | 0.0230 μs |  1.83 |    0.01 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  5.146 μs | 0.1883 μs | 0.0103 μs |  1.21 |    0.00 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 25.119 μs | 1.5107 μs | 0.0828 μs |  5.89 |    0.02 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
