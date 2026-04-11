```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     | 3           |  4.338 μs | 0.0232 μs | 0.0153 μs |  1.00 |    0.00 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     | 3           |  6.648 μs | 0.0317 μs | 0.0210 μs |  1.53 |    0.01 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     | 3           |  5.324 μs | 0.0269 μs | 0.0160 μs |  1.23 |    0.01 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     | 3           |  8.046 μs | 0.0299 μs | 0.0198 μs |  1.86 |    0.01 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     | 3           |  5.172 μs | 0.0188 μs | 0.0124 μs |  1.19 |    0.00 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 3           | 26.004 μs | 0.1057 μs | 0.0553 μs |  6.00 |    0.02 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |             |           |           |           |       |         |        |        |           |             |
| SingleHandler              | MediumRun  | 15             | 2           | 10          |  4.246 μs | 0.0406 μs | 0.0583 μs |  1.00 |    0.02 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | MediumRun  | 15             | 2           | 10          |  6.710 μs | 0.0402 μs | 0.0601 μs |  1.58 |    0.03 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | MediumRun  | 15             | 2           | 10          |  5.277 μs | 0.0122 μs | 0.0183 μs |  1.24 |    0.02 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | MediumRun  | 15             | 2           | 10          |  8.097 μs | 0.0591 μs | 0.0867 μs |  1.91 |    0.03 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | MediumRun  | 15             | 2           | 10          |  5.211 μs | 0.0189 μs | 0.0283 μs |  1.23 |    0.02 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | MediumRun  | 15             | 2           | 10          | 25.457 μs | 0.0653 μs | 0.0978 μs |  6.00 |    0.08 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
