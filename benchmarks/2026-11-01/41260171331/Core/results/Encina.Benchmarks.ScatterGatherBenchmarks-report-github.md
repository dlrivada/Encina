```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     | 3           |  2.593 μs | 0.0761 μs | 0.0503 μs |  1.00 |    0.03 | 0.1564 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     | 3           |  3.995 μs | 0.1160 μs | 0.0690 μs |  1.54 |    0.04 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     | 3           |  3.322 μs | 0.0703 μs | 0.0465 μs |  1.28 |    0.03 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     | 3           |  4.786 μs | 0.0892 μs | 0.0531 μs |  1.85 |    0.04 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     | 3           |  3.197 μs | 0.0943 μs | 0.0623 μs |  1.23 |    0.03 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 3           | 15.438 μs | 0.2411 μs | 0.1261 μs |  5.96 |    0.12 | 1.1902 | 0.0763 |  19.69 KB |        7.64 |
|                            |            |                |             |             |           |           |           |       |         |        |        |           |             |
| SingleHandler              | MediumRun  | 15             | 2           | 10          |  2.606 μs | 0.0136 μs | 0.0195 μs |  1.00 |    0.01 | 0.1564 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | MediumRun  | 15             | 2           | 10          |  4.150 μs | 0.0282 μs | 0.0395 μs |  1.59 |    0.02 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | MediumRun  | 15             | 2           | 10          |  3.404 μs | 0.0482 μs | 0.0706 μs |  1.31 |    0.03 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | MediumRun  | 15             | 2           | 10          |  4.942 μs | 0.0810 μs | 0.1188 μs |  1.90 |    0.05 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | MediumRun  | 15             | 2           | 10          |  3.210 μs | 0.0272 μs | 0.0382 μs |  1.23 |    0.02 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | MediumRun  | 15             | 2           | 10          | 16.200 μs | 0.0854 μs | 0.1252 μs |  6.22 |    0.07 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
