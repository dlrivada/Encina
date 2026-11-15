```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.89GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     | 3           |  3.694 μs | 0.0999 μs | 0.0661 μs |  1.00 |    0.02 | 0.0305 |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     | 3           |  6.369 μs | 0.0871 μs | 0.0576 μs |  1.72 |    0.03 | 0.0458 |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     | 3           |  4.999 μs | 0.0310 μs | 0.0185 μs |  1.35 |    0.02 | 0.0381 |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     | 3           |  7.298 μs | 0.0157 μs | 0.0082 μs |  1.98 |    0.03 | 0.0610 |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     | 3           |  4.622 μs | 0.0422 μs | 0.0251 μs |  1.25 |    0.02 | 0.0381 |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 3           | 24.814 μs | 0.0788 μs | 0.0469 μs |  6.72 |    0.11 | 0.2136 |  19.69 KB |        7.64 |
|                            |            |                |             |             |           |           |           |       |         |        |           |             |
| SingleHandler              | MediumRun  | 15             | 2           | 10          |  3.692 μs | 0.0279 μs | 0.0400 μs |  1.00 |    0.01 | 0.0305 |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | MediumRun  | 15             | 2           | 10          |  6.369 μs | 0.0354 μs | 0.0518 μs |  1.73 |    0.02 | 0.0458 |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | MediumRun  | 15             | 2           | 10          |  5.002 μs | 0.0369 μs | 0.0553 μs |  1.35 |    0.02 | 0.0381 |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | MediumRun  | 15             | 2           | 10          |  7.495 μs | 0.0453 μs | 0.0649 μs |  2.03 |    0.03 | 0.0610 |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | MediumRun  | 15             | 2           | 10          |  4.544 μs | 0.0248 μs | 0.0348 μs |  1.23 |    0.02 | 0.0381 |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | MediumRun  | 15             | 2           | 10          | 25.212 μs | 0.0771 μs | 0.1030 μs |  6.83 |    0.08 | 0.2136 |  19.69 KB |        7.64 |
