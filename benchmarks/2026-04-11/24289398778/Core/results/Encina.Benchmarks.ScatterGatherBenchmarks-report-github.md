```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     | 3           |  4.432 μs | 0.0288 μs | 0.0190 μs |  4.429 μs |  1.00 |    0.01 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     | 3           |  6.697 μs | 0.0429 μs | 0.0256 μs |  6.688 μs |  1.51 |    0.01 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     | 3           |  5.448 μs | 0.0401 μs | 0.0266 μs |  5.457 μs |  1.23 |    0.01 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     | 3           |  8.016 μs | 0.0254 μs | 0.0168 μs |  8.010 μs |  1.81 |    0.01 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     | 3           |  5.247 μs | 0.0087 μs | 0.0052 μs |  5.245 μs |  1.18 |    0.00 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 3           | 26.082 μs | 0.1869 μs | 0.1236 μs | 26.036 μs |  5.88 |    0.04 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
|                            |            |                |             |             |           |           |           |           |       |         |        |        |           |             |
| SingleHandler              | MediumRun  | 15             | 2           | 10          |  4.389 μs | 0.0273 μs | 0.0374 μs |  4.382 μs |  1.00 |    0.01 | 0.1526 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | MediumRun  | 15             | 2           | 10          |  6.723 μs | 0.0248 μs | 0.0340 μs |  6.725 μs |  1.53 |    0.01 | 0.2518 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | MediumRun  | 15             | 2           | 10          |  5.412 μs | 0.0312 μs | 0.0437 μs |  5.437 μs |  1.23 |    0.01 | 0.1907 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | MediumRun  | 15             | 2           | 10          |  8.076 μs | 0.0552 μs | 0.0774 μs |  8.028 μs |  1.84 |    0.02 | 0.3357 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | MediumRun  | 15             | 2           | 10          |  5.291 μs | 0.0124 μs | 0.0182 μs |  5.292 μs |  1.21 |    0.01 | 0.2060 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | MediumRun  | 15             | 2           | 10          | 26.218 μs | 0.0747 μs | 0.0997 μs | 26.237 μs |  5.97 |    0.05 | 1.1902 | 0.0610 |  19.69 KB |        7.64 |
