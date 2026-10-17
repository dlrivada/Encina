```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  4.429 μs | 0.0469 μs | 0.0279 μs |  1.00 |    0.01 | 0.0992 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  6.876 μs | 0.0235 μs | 0.0140 μs |  1.55 |    0.01 | 0.1678 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  5.260 μs | 0.0174 μs | 0.0103 μs |  1.19 |    0.01 | 0.1221 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  8.250 μs | 0.0301 μs | 0.0199 μs |  1.86 |    0.01 | 0.2136 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  5.412 μs | 0.0278 μs | 0.0184 μs |  1.22 |    0.01 | 0.1373 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 26.515 μs | 0.0925 μs | 0.0612 μs |  5.99 |    0.04 | 0.7935 | 0.0305 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |         |        |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  4.447 μs | 0.1270 μs | 0.0070 μs |  1.00 |    0.00 | 0.0992 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  6.901 μs | 0.6684 μs | 0.0366 μs |  1.55 |    0.01 | 0.1678 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  5.252 μs | 0.5078 μs | 0.0278 μs |  1.18 |    0.01 | 0.1221 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  8.386 μs | 2.4818 μs | 0.1360 μs |  1.89 |    0.03 | 0.2136 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  5.264 μs | 0.9476 μs | 0.0519 μs |  1.18 |    0.01 | 0.1373 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 26.388 μs | 2.4794 μs | 0.1359 μs |  5.93 |    0.03 | 0.7935 | 0.0305 |  19.69 KB |        7.64 |
