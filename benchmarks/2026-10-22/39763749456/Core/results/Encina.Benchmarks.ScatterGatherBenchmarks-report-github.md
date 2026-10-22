```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  2.735 μs | 0.1337 μs | 0.0796 μs |  1.00 |    0.04 | 0.0305 |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  4.650 μs | 0.0151 μs | 0.0079 μs |  1.70 |    0.05 | 0.0458 |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  3.725 μs | 0.0507 μs | 0.0265 μs |  1.36 |    0.04 | 0.0381 |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  5.673 μs | 0.2552 μs | 0.1519 μs |  2.08 |    0.08 | 0.0610 |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  3.377 μs | 0.0664 μs | 0.0395 μs |  1.24 |    0.04 | 0.0381 |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 18.809 μs | 0.1065 μs | 0.0705 μs |  6.88 |    0.19 | 0.2136 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  3.116 μs | 0.5759 μs | 0.0316 μs |  1.00 |    0.01 | 0.0305 |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  5.022 μs | 5.9239 μs | 0.3247 μs |  1.61 |    0.09 | 0.0458 |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  3.763 μs | 0.8633 μs | 0.0473 μs |  1.21 |    0.02 | 0.0381 |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  5.617 μs | 4.1927 μs | 0.2298 μs |  1.80 |    0.07 | 0.0610 |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  3.348 μs | 0.2020 μs | 0.0111 μs |  1.07 |    0.01 | 0.0381 |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 18.652 μs | 0.8561 μs | 0.0469 μs |  5.99 |    0.05 | 0.2136 |  19.69 KB |        7.64 |
