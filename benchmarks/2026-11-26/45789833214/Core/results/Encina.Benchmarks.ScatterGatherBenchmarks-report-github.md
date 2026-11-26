```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  2.885 μs | 0.0131 μs | 0.0087 μs |  1.00 |    0.00 | 0.0305 |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  5.008 μs | 0.0202 μs | 0.0133 μs |  1.74 |    0.01 | 0.0458 |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  4.042 μs | 0.0202 μs | 0.0134 μs |  1.40 |    0.01 | 0.0381 |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  5.937 μs | 0.0270 μs | 0.0161 μs |  2.06 |    0.01 | 0.0610 |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  3.602 μs | 0.0108 μs | 0.0072 μs |  1.25 |    0.00 | 0.0381 |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 20.380 μs | 0.1493 μs | 0.0987 μs |  7.06 |    0.04 | 0.2136 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  2.948 μs | 0.1400 μs | 0.0077 μs |  1.00 |    0.00 | 0.0305 |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  5.036 μs | 0.1152 μs | 0.0063 μs |  1.71 |    0.00 | 0.0458 |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  4.025 μs | 0.1002 μs | 0.0055 μs |  1.37 |    0.00 | 0.0381 |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  5.948 μs | 0.2142 μs | 0.0117 μs |  2.02 |    0.01 | 0.0610 |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  3.700 μs | 0.2663 μs | 0.0146 μs |  1.25 |    0.01 | 0.0381 |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 20.316 μs | 1.6814 μs | 0.0922 μs |  6.89 |    0.03 | 0.2136 |  19.69 KB |        7.64 |
