```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  4.257 μs | 0.0125 μs | 0.0074 μs |  1.00 |    0.00 | 0.0992 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  6.676 μs | 0.0388 μs | 0.0231 μs |  1.57 |    0.01 | 0.1678 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  5.085 μs | 0.0275 μs | 0.0164 μs |  1.19 |    0.00 | 0.1221 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  8.035 μs | 0.0336 μs | 0.0175 μs |  1.89 |    0.00 | 0.2136 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  5.071 μs | 0.0213 μs | 0.0111 μs |  1.19 |    0.00 | 0.1373 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 26.780 μs | 0.1901 μs | 0.0994 μs |  6.29 |    0.02 | 0.7935 | 0.0305 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |         |        |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  4.230 μs | 0.1187 μs | 0.0065 μs |  1.00 |    0.00 | 0.0992 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  6.649 μs | 0.4227 μs | 0.0232 μs |  1.57 |    0.01 | 0.1678 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  4.980 μs | 0.5644 μs | 0.0309 μs |  1.18 |    0.01 | 0.1221 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  7.945 μs | 0.3949 μs | 0.0216 μs |  1.88 |    0.01 | 0.2136 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  5.121 μs | 0.1194 μs | 0.0065 μs |  1.21 |    0.00 | 0.1373 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 25.694 μs | 1.2314 μs | 0.0675 μs |  6.07 |    0.02 | 0.7935 | 0.0305 |  19.69 KB |        7.64 |
