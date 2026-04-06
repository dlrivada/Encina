```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.90GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     |  4.288 μs | 0.0123 μs | 0.0073 μs |  1.00 |    0.00 | 0.0992 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     |  6.802 μs | 0.0255 μs | 0.0168 μs |  1.59 |    0.00 | 0.1678 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     |  4.990 μs | 0.0158 μs | 0.0094 μs |  1.16 |    0.00 | 0.1221 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     |  7.811 μs | 0.0268 μs | 0.0159 μs |  1.82 |    0.00 | 0.2136 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     |  5.052 μs | 0.0305 μs | 0.0202 μs |  1.18 |    0.00 | 0.1373 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 24.899 μs | 1.6674 μs | 1.1029 μs |  5.81 |    0.25 | 0.7935 | 0.0305 |  19.69 KB |        7.64 |
|                            |            |                |             |           |           |           |       |         |        |        |           |             |
| SingleHandler              | ShortRun   | 3              | 1           |  4.703 μs | 2.7719 μs | 0.1519 μs |  1.00 |    0.04 | 0.0992 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | ShortRun   | 3              | 1           |  6.757 μs | 2.4874 μs | 0.1363 μs |  1.44 |    0.05 | 0.1678 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | ShortRun   | 3              | 1           |  5.001 μs | 0.3482 μs | 0.0191 μs |  1.06 |    0.03 | 0.1221 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | ShortRun   | 3              | 1           |  7.964 μs | 0.6450 μs | 0.0354 μs |  1.69 |    0.05 | 0.2136 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | ShortRun   | 3              | 1           |  5.122 μs | 0.1006 μs | 0.0055 μs |  1.09 |    0.03 | 0.1373 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | ShortRun   | 3              | 1           | 24.649 μs | 1.3975 μs | 0.0766 μs |  5.24 |    0.15 | 0.7935 | 0.0305 |  19.69 KB |        7.64 |
