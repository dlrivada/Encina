```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  2.494 μs | 0.0373 μs | 0.0222 μs |  1.00 |    0.01 | 0.1564 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  3.961 μs | 0.1069 μs | 0.0707 μs |  1.59 |    0.03 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  3.195 μs | 0.0586 μs | 0.0348 μs |  1.28 |    0.02 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  4.509 μs | 0.0691 μs | 0.0411 μs |  1.81 |    0.02 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  2.990 μs | 0.1038 μs | 0.0687 μs |  1.20 |    0.03 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 15.311 μs | 0.5624 μs | 0.3347 μs |  6.14 |    0.14 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |         |        |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  2.483 μs | 1.4734 μs | 0.0808 μs |  1.00 |    0.04 | 0.1564 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  3.974 μs | 0.5216 μs | 0.0286 μs |  1.60 |    0.05 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  3.266 μs | 0.1281 μs | 0.0070 μs |  1.32 |    0.04 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  4.599 μs | 1.0214 μs | 0.0560 μs |  1.85 |    0.05 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  2.956 μs | 0.5687 μs | 0.0312 μs |  1.19 |    0.03 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 15.149 μs | 4.0357 μs | 0.2212 μs |  6.10 |    0.19 | 1.1902 | 0.0763 |  19.69 KB |        7.64 |
