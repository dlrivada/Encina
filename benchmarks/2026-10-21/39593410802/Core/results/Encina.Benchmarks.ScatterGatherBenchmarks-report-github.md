```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.36GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  2.398 μs |  0.0370 μs | 0.0220 μs |  1.00 |    0.01 | 0.1564 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  3.827 μs |  0.0586 μs | 0.0307 μs |  1.60 |    0.02 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  3.179 μs |  0.0893 μs | 0.0591 μs |  1.33 |    0.03 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  4.493 μs |  0.0504 μs | 0.0334 μs |  1.87 |    0.02 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  3.026 μs |  0.0684 μs | 0.0357 μs |  1.26 |    0.02 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 14.986 μs |  0.3720 μs | 0.2460 μs |  6.25 |    0.11 | 1.1902 | 0.0763 |  19.69 KB |        7.64 |
|                            |            |                |             |           |            |           |       |         |        |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  2.851 μs | 11.0208 μs | 0.6041 μs |  1.03 |    0.25 | 0.1564 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  4.042 μs |  5.6908 μs | 0.3119 μs |  1.46 |    0.26 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  3.129 μs |  0.2312 μs | 0.0127 μs |  1.13 |    0.19 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  4.887 μs |  1.9400 μs | 0.1063 μs |  1.76 |    0.29 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  3.063 μs |  1.4330 μs | 0.0785 μs |  1.10 |    0.18 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 15.073 μs |  3.1106 μs | 0.1705 μs |  5.43 |    0.90 | 1.1902 | 0.0763 |  19.69 KB |        7.64 |
