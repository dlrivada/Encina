```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  2.866 μs | 0.0257 μs | 0.0170 μs |  1.00 |    0.01 | 0.0305 |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  4.948 μs | 0.0442 μs | 0.0293 μs |  1.73 |    0.01 | 0.0458 |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  3.989 μs | 0.0315 μs | 0.0208 μs |  1.39 |    0.01 | 0.0381 |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  5.920 μs | 0.0769 μs | 0.0509 μs |  2.07 |    0.02 | 0.0610 |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  3.578 μs | 0.0181 μs | 0.0120 μs |  1.25 |    0.01 | 0.0381 |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 20.058 μs | 0.1812 μs | 0.0947 μs |  7.00 |    0.05 | 0.2136 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  2.836 μs | 0.3042 μs | 0.0167 μs |  1.00 |    0.01 | 0.0305 |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  4.932 μs | 0.2124 μs | 0.0116 μs |  1.74 |    0.01 | 0.0458 |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  3.929 μs | 0.1961 μs | 0.0107 μs |  1.39 |    0.01 | 0.0381 |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  5.892 μs | 0.5485 μs | 0.0301 μs |  2.08 |    0.01 | 0.0610 |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  3.554 μs | 0.0638 μs | 0.0035 μs |  1.25 |    0.01 | 0.0381 |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 20.201 μs | 2.4460 μs | 0.1341 μs |  7.12 |    0.05 | 0.2136 |  19.69 KB |        7.64 |
