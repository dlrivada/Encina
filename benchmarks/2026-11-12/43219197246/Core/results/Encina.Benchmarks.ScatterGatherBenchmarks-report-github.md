```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  3.676 μs | 0.0171 μs | 0.0102 μs |  1.00 |    0.00 | 0.0305 |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  6.334 μs | 0.0272 μs | 0.0142 μs |  1.72 |    0.01 | 0.0458 |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  5.071 μs | 0.0660 μs | 0.0393 μs |  1.38 |    0.01 | 0.0381 |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  7.454 μs | 0.0670 μs | 0.0443 μs |  2.03 |    0.01 | 0.0610 |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  4.586 μs | 0.0251 μs | 0.0166 μs |  1.25 |    0.01 | 0.0381 |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 24.861 μs | 0.3251 μs | 0.1935 μs |  6.76 |    0.05 | 0.2136 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  3.700 μs | 1.4991 μs | 0.0822 μs |  1.00 |    0.03 | 0.0305 |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  6.306 μs | 0.3551 μs | 0.0195 μs |  1.70 |    0.03 | 0.0458 |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  5.018 μs | 0.4396 μs | 0.0241 μs |  1.36 |    0.03 | 0.0381 |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  7.394 μs | 1.0470 μs | 0.0574 μs |  2.00 |    0.04 | 0.0610 |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  4.537 μs | 0.7321 μs | 0.0401 μs |  1.23 |    0.03 | 0.0381 |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 24.947 μs | 1.2964 μs | 0.0711 μs |  6.75 |    0.13 | 0.2136 |  19.69 KB |        7.64 |
