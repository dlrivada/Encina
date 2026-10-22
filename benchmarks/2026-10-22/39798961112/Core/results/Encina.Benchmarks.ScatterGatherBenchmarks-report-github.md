```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     | 3           |  4.437 μs | 0.0068 μs | 0.0045 μs |  4.436 μs |  1.00 |    0.00 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     | 3           |  6.841 μs | 0.0514 μs | 0.0306 μs |  6.826 μs |  1.54 |    0.01 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     | 3           |  5.513 μs | 0.0185 μs | 0.0122 μs |  5.509 μs |  1.24 |    0.00 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     | 3           |  8.233 μs | 0.0197 μs | 0.0117 μs |  8.231 μs |  1.86 |    0.00 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     | 3           |  5.386 μs | 0.0118 μs | 0.0070 μs |  5.385 μs |  1.21 |    0.00 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 3           | 25.541 μs | 0.0868 μs | 0.0516 μs | 25.556 μs |  5.76 |    0.01 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |             |           |           |           |           |       |         |        |        |           |             |
| SingleHandler              | MediumRun  | 15             | 2           | 10          |  4.453 μs | 0.0288 μs | 0.0404 μs |  4.436 μs |  1.00 |    0.01 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | MediumRun  | 15             | 2           | 10          |  6.923 μs | 0.0091 μs | 0.0127 μs |  6.925 μs |  1.55 |    0.01 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | MediumRun  | 15             | 2           | 10          |  5.486 μs | 0.0128 μs | 0.0183 μs |  5.487 μs |  1.23 |    0.01 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | MediumRun  | 15             | 2           | 10          |  8.134 μs | 0.0272 μs | 0.0390 μs |  8.141 μs |  1.83 |    0.02 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | MediumRun  | 15             | 2           | 10          |  5.330 μs | 0.0421 μs | 0.0604 μs |  5.288 μs |  1.20 |    0.02 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | MediumRun  | 15             | 2           | 10          | 26.084 μs | 0.1067 μs | 0.1530 μs | 26.096 μs |  5.86 |    0.06 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
