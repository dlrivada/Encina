```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  4.400 μs | 0.0249 μs | 0.0165 μs |  1.00 |    0.01 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  6.924 μs | 0.0479 μs | 0.0285 μs |  1.57 |    0.01 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  5.418 μs | 0.0245 μs | 0.0162 μs |  1.23 |    0.01 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  7.997 μs | 0.0265 μs | 0.0158 μs |  1.82 |    0.01 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  5.300 μs | 0.0356 μs | 0.0236 μs |  1.20 |    0.01 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 25.578 μs | 0.1556 μs | 0.0926 μs |  5.81 |    0.03 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |         |        |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  4.376 μs | 0.0745 μs | 0.0041 μs |  1.00 |    0.00 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  6.679 μs | 0.4010 μs | 0.0220 μs |  1.53 |    0.00 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  5.499 μs | 0.3177 μs | 0.0174 μs |  1.26 |    0.00 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  7.952 μs | 0.4026 μs | 0.0221 μs |  1.82 |    0.00 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  5.221 μs | 0.4926 μs | 0.0270 μs |  1.19 |    0.01 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 25.376 μs | 1.0157 μs | 0.0557 μs |  5.80 |    0.01 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
