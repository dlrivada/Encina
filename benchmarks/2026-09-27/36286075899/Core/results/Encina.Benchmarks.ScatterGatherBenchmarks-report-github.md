```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SingleHandler              | Job-YFEFPZ | 10             | Default     | 3           |  4.209 μs | 0.0111 μs | 0.0066 μs |  4.208 μs |  1.00 |    0.00 | 0.0992 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | Job-YFEFPZ | 10             | Default     | 3           |  6.557 μs | 0.0182 μs | 0.0120 μs |  6.556 μs |  1.56 |    0.00 | 0.1678 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | Job-YFEFPZ | 10             | Default     | 3           |  5.051 μs | 0.0188 μs | 0.0112 μs |  5.052 μs |  1.20 |    0.00 | 0.1221 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | Job-YFEFPZ | 10             | Default     | 3           |  7.778 μs | 0.0191 μs | 0.0114 μs |  7.781 μs |  1.85 |    0.00 | 0.2136 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | Job-YFEFPZ | 10             | Default     | 3           |  5.037 μs | 0.0206 μs | 0.0137 μs |  5.033 μs |  1.20 |    0.00 | 0.1373 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | Job-YFEFPZ | 10             | Default     | 3           | 24.712 μs | 0.0391 μs | 0.0232 μs | 24.714 μs |  5.87 |    0.01 | 0.7935 | 0.0305 |  19.69 KB |        7.64 |
|                            |            |                |             |             |           |           |           |           |       |         |        |        |           |             |
| SingleHandler              | MediumRun  | 15             | 2           | 10          |  4.307 μs | 0.0335 μs | 0.0469 μs |  4.283 μs |  1.00 |    0.02 | 0.0992 |      - |   2.58 KB |        1.00 |
| FiveHandlers_Parallel      | MediumRun  | 15             | 2           | 10          |  6.854 μs | 0.0558 μs | 0.0835 μs |  6.835 μs |  1.59 |    0.03 | 0.1678 |      - |   4.16 KB |        1.61 |
| FiveHandlers_Sequential    | MediumRun  | 15             | 2           | 10          |  4.959 μs | 0.0109 μs | 0.0160 μs |  4.954 μs |  1.15 |    0.01 | 0.1221 |      - |   3.16 KB |        1.22 |
| TenHandlers_Quorum         | MediumRun  | 15             | 2           | 10          |  7.841 μs | 0.0451 μs | 0.0661 μs |  7.868 μs |  1.82 |    0.02 | 0.2136 |      - |   5.52 KB |        2.14 |
| ThreeHandlers_WaitForFirst | MediumRun  | 15             | 2           | 10          |  5.083 μs | 0.0245 μs | 0.0360 μs |  5.058 μs |  1.18 |    0.02 | 0.1373 |      - |   3.39 KB |        1.32 |
| FiftyHandlers_Parallel     | MediumRun  | 15             | 2           | 10          | 26.026 μs | 0.4978 μs | 0.7296 μs | 25.606 μs |  6.04 |    0.18 | 0.7935 | 0.0305 |  19.69 KB |        7.64 |
