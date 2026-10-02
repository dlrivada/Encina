```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **8.424 μs** |     **0.4711 μs** |   **0.2803 μs** |  **1.00** |    **0.05** | **0.0153** |      **-** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   8.978 μs |     0.0333 μs |   0.0198 μs |  1.07 |    0.03 | 0.0305 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  86.065 μs |     3.7911 μs |   2.2560 μs | 10.23 |    0.41 | 0.3662 | 0.2441 |  37.79 KB |       19.12 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  46.184 μs |     0.4143 μs |   0.2465 μs |  5.49 |    0.18 | 0.1221 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   8.475 μs |     3.4090 μs |   0.1869 μs |  1.00 |    0.03 | 0.0153 |      - |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   9.067 μs |     0.3204 μs |   0.0176 μs |  1.07 |    0.02 | 0.0305 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 103.139 μs |   550.6961 μs |  30.1855 μs | 12.17 |    3.10 | 0.1221 |      - |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  46.895 μs |    11.1920 μs |   0.6135 μs |  5.54 |    0.12 | 0.1221 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **66.672 μs** |     **3.5778 μs** |   **1.8713 μs** |     **?** |       **?** | **0.1221** |      **-** |  **17.62 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  74.337 μs |   268.9503 μs |  14.7421 μs |     ? |       ? | 0.1221 |      - |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **368.411 μs** |    **11.4543 μs** |   **5.9908 μs** |     **?** |       **?** | **0.9766** | **0.4883** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 398.998 μs |   951.3721 μs |  52.1479 μs |     ? |       ? | 0.9766 | 0.4883 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **741.970 μs** |     **9.3681 μs** |   **4.8997 μs** |     **?** |       **?** | **1.9531** | **0.9766** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 788.084 μs | 2,020.8154 μs | 110.7677 μs |     ? |       ? | 1.9531 | 0.9766 | 174.78 KB |           ? |
