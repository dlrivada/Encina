```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     | 3           |  3.423 μs | 0.0183 μs | 0.0095 μs |  1.00 |    0.00 | 0.1564 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     | 3           |  5.448 μs | 0.0233 μs | 0.0122 μs |  1.59 |    0.01 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     | 3           |  4.342 μs | 0.0164 μs | 0.0098 μs |  1.27 |    0.00 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     | 3           |  6.447 μs | 0.0996 μs | 0.0659 μs |  1.88 |    0.02 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     | 3           |  4.014 μs | 0.0433 μs | 0.0258 μs |  1.17 |    0.01 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 3           | 19.800 μs | 0.3062 μs | 0.2026 μs |  5.78 |    0.06 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |             |           |           |           |       |         |        |        |           |             |
| SingleHandler              | MediumRun  | 15             | 2           | 10          |  3.402 μs | 0.0168 μs | 0.0241 μs |  1.00 |    0.01 | 0.1564 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | MediumRun  | 15             | 2           | 10          |  5.248 μs | 0.0377 μs | 0.0540 μs |  1.54 |    0.02 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | MediumRun  | 15             | 2           | 10          |  4.286 μs | 0.0184 μs | 0.0269 μs |  1.26 |    0.01 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | MediumRun  | 15             | 2           | 10          |  6.378 μs | 0.0304 μs | 0.0427 μs |  1.87 |    0.02 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | MediumRun  | 15             | 2           | 10          |  4.127 μs | 0.0333 μs | 0.0467 μs |  1.21 |    0.02 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | MediumRun  | 15             | 2           | 10          | 20.273 μs | 0.1679 μs | 0.2354 μs |  5.96 |    0.08 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
