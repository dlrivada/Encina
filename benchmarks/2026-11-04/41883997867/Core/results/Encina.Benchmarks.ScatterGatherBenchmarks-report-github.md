```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.40GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  4.287 μs | 0.0392 μs | 0.0233 μs |  1.00 |    0.01 | 0.0992 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  6.531 μs | 0.0189 μs | 0.0125 μs |  1.52 |    0.01 | 0.1678 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  4.973 μs | 0.0110 μs | 0.0073 μs |  1.16 |    0.01 | 0.1221 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  7.689 μs | 0.0257 μs | 0.0170 μs |  1.79 |    0.01 | 0.2136 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  5.289 μs | 0.0118 μs | 0.0078 μs |  1.23 |    0.01 | 0.1373 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 24.558 μs | 0.0831 μs | 0.0550 μs |  5.73 |    0.03 | 0.7935 | 0.0305 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |         |        |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  4.264 μs | 1.7159 μs | 0.0941 μs |  1.00 |    0.03 | 0.0992 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  6.617 μs | 0.1570 μs | 0.0086 μs |  1.55 |    0.03 | 0.1678 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  4.888 μs | 0.2582 μs | 0.0142 μs |  1.15 |    0.02 | 0.1221 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  7.838 μs | 0.6900 μs | 0.0378 μs |  1.84 |    0.04 | 0.2136 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  4.941 μs | 0.5017 μs | 0.0275 μs |  1.16 |    0.02 | 0.1373 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 25.776 μs | 0.5216 μs | 0.0286 μs |  6.05 |    0.11 | 0.7935 | 0.0305 |  19.69 KB |        7.64 |
