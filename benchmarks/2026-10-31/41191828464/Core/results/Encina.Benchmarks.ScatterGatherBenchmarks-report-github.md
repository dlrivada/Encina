```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  4.459 μs | 0.0118 μs | 0.0070 μs |  1.00 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  6.879 μs | 0.0177 μs | 0.0106 μs |  1.54 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  5.595 μs | 0.0173 μs | 0.0115 μs |  1.25 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  8.325 μs | 0.0236 μs | 0.0156 μs |  1.87 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  5.420 μs | 0.0129 μs | 0.0077 μs |  1.22 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 25.855 μs | 0.0559 μs | 0.0333 μs |  5.80 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |        |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  4.478 μs | 0.1700 μs | 0.0093 μs |  1.00 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  6.930 μs | 0.6404 μs | 0.0351 μs |  1.55 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  5.473 μs | 0.0289 μs | 0.0016 μs |  1.22 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  8.171 μs | 0.0827 μs | 0.0045 μs |  1.82 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  5.279 μs | 0.7903 μs | 0.0433 μs |  1.18 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 25.482 μs | 0.7733 μs | 0.0424 μs |  5.69 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
