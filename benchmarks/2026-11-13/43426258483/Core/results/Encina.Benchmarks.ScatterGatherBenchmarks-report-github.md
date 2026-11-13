```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  2.540 μs |  0.0724 μs | 0.0431 μs |  1.00 |    0.02 | 0.1564 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  3.953 μs |  0.0486 μs | 0.0289 μs |  1.56 |    0.03 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  3.332 μs |  0.1143 μs | 0.0680 μs |  1.31 |    0.03 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  4.695 μs |  0.0638 μs | 0.0380 μs |  1.85 |    0.03 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  3.087 μs |  0.0921 μs | 0.0609 μs |  1.22 |    0.03 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 15.573 μs |  0.2791 μs | 0.1846 μs |  6.13 |    0.12 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |           |            |           |       |         |        |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  2.573 μs |  0.1233 μs | 0.0068 μs |  1.00 |    0.00 | 0.1564 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  3.968 μs |  0.8306 μs | 0.0455 μs |  1.54 |    0.02 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  3.287 μs |  0.5424 μs | 0.0297 μs |  1.28 |    0.01 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  4.639 μs |  1.7010 μs | 0.0932 μs |  1.80 |    0.03 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  3.152 μs |  0.3354 μs | 0.0184 μs |  1.22 |    0.01 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 16.479 μs | 14.6022 μs | 0.8004 μs |  6.40 |    0.27 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
