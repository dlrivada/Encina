```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3


```
| Method                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     | 3           |  4.331 μs | 0.0161 μs | 0.0096 μs |  4.329 μs |  1.00 |    0.00 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     | 3           |  6.642 μs | 0.0239 μs | 0.0142 μs |  6.645 μs |  1.53 |    0.00 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     | 3           |  5.418 μs | 0.0162 μs | 0.0107 μs |  5.421 μs |  1.25 |    0.00 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     | 3           |  8.105 μs | 0.0355 μs | 0.0211 μs |  8.113 μs |  1.87 |    0.01 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     | 3           |  5.116 μs | 0.0449 μs | 0.0297 μs |  5.105 μs |  1.18 |    0.01 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 3           | 25.505 μs | 0.0640 μs | 0.0381 μs | 25.518 μs |  5.89 |    0.01 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |             |           |           |           |           |       |         |        |        |           |             |
| SingleHandler              | MediumRun  | 15             | 2           | 10          |  4.310 μs | 0.0364 μs | 0.0533 μs |  4.343 μs |  1.00 |    0.02 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | MediumRun  | 15             | 2           | 10          |  6.779 μs | 0.0375 μs | 0.0526 μs |  6.806 μs |  1.57 |    0.02 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | MediumRun  | 15             | 2           | 10          |  5.349 μs | 0.0174 μs | 0.0255 μs |  5.341 μs |  1.24 |    0.02 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | MediumRun  | 15             | 2           | 10          |  7.933 μs | 0.0170 μs | 0.0249 μs |  7.932 μs |  1.84 |    0.02 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | MediumRun  | 15             | 2           | 10          |  5.206 μs | 0.0339 μs | 0.0496 μs |  5.233 μs |  1.21 |    0.02 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | MediumRun  | 15             | 2           | 10          | 25.909 μs | 0.1187 μs | 0.1740 μs | 25.897 μs |  6.01 |    0.08 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
