```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  2.820 μs | 0.2985 μs | 0.1974 μs |  1.00 |    0.09 | 0.0305 |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  4.648 μs | 0.0161 μs | 0.0084 μs |  1.66 |    0.10 | 0.0458 |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  3.681 μs | 0.0068 μs | 0.0036 μs |  1.31 |    0.08 | 0.0381 |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  5.485 μs | 0.0286 μs | 0.0189 μs |  1.95 |    0.12 | 0.0610 |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  3.366 μs | 0.0225 μs | 0.0134 μs |  1.20 |    0.08 | 0.0381 |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 18.475 μs | 0.0635 μs | 0.0420 μs |  6.58 |    0.42 | 0.2136 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  3.075 μs | 0.4182 μs | 0.0229 μs |  1.00 |    0.01 | 0.0305 |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  4.645 μs | 0.0251 μs | 0.0014 μs |  1.51 |    0.01 | 0.0458 |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  3.669 μs | 0.2430 μs | 0.0133 μs |  1.19 |    0.01 | 0.0381 |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  5.552 μs | 0.2796 μs | 0.0153 μs |  1.81 |    0.01 | 0.0610 |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  3.762 μs | 3.9279 μs | 0.2153 μs |  1.22 |    0.06 | 0.0381 |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 18.406 μs | 0.4502 μs | 0.0247 μs |  5.99 |    0.04 | 0.2136 |  19.69 KB |        7.64 |
