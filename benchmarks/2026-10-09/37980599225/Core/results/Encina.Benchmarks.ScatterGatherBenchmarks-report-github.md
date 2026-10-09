```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  4.147 μs | 0.0086 μs | 0.0051 μs |  1.00 |    0.00 | 0.0992 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  6.674 μs | 0.0411 μs | 0.0272 μs |  1.61 |    0.01 | 0.1678 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  5.207 μs | 0.0335 μs | 0.0199 μs |  1.26 |    0.00 | 0.1221 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  7.881 μs | 0.0286 μs | 0.0189 μs |  1.90 |    0.00 | 0.2136 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  5.105 μs | 0.0320 μs | 0.0212 μs |  1.23 |    0.01 | 0.1373 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 24.739 μs | 0.1484 μs | 0.0883 μs |  5.97 |    0.02 | 0.7935 | 0.0305 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |         |        |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  4.288 μs | 0.1702 μs | 0.0093 μs |  1.00 |    0.00 | 0.0992 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  6.866 μs | 0.0413 μs | 0.0023 μs |  1.60 |    0.00 | 0.1678 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  4.942 μs | 0.1611 μs | 0.0088 μs |  1.15 |    0.00 | 0.1221 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  7.967 μs | 0.1047 μs | 0.0057 μs |  1.86 |    0.00 | 0.2136 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  5.131 μs | 0.0531 μs | 0.0029 μs |  1.20 |    0.00 | 0.1373 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 24.748 μs | 0.5239 μs | 0.0287 μs |  5.77 |    0.01 | 0.7935 | 0.0305 |  19.69 KB |        7.64 |
