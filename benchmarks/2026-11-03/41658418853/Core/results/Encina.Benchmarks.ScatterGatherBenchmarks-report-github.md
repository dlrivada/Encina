```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  4.605 μs | 0.0134 μs | 0.0079 μs |  1.00 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  6.948 μs | 0.0235 μs | 0.0155 μs |  1.51 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  5.615 μs | 0.0097 μs | 0.0064 μs |  1.22 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  8.300 μs | 0.0395 μs | 0.0207 μs |  1.80 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  5.502 μs | 0.0069 μs | 0.0041 μs |  1.19 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 26.102 μs | 0.0497 μs | 0.0296 μs |  5.67 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |        |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  4.630 μs | 0.0468 μs | 0.0026 μs |  1.00 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  6.965 μs | 0.1025 μs | 0.0056 μs |  1.50 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  5.567 μs | 0.0452 μs | 0.0025 μs |  1.20 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  8.222 μs | 0.3316 μs | 0.0182 μs |  1.78 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  5.335 μs | 0.5823 μs | 0.0319 μs |  1.15 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 26.556 μs | 0.2682 μs | 0.0147 μs |  5.74 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
