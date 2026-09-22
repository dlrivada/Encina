```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error       | StdDev     | Median     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|------------:|-----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **8.777 μs** |   **0.8546 μs** |  **0.5085 μs** |   **8.881 μs** |  **1.00** |    **0.08** | **0.0229** | **0.0153** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   6.350 μs |   0.0174 μs |  0.0091 μs |   6.350 μs |  0.73 |    0.04 | 0.0305 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  67.693 μs |   2.6190 μs |  1.5585 μs |  68.034 μs |  7.74 |    0.46 | 0.3662 | 0.2441 |   37.8 KB |       19.12 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  32.068 μs |   0.3715 μs |  0.1943 μs |  32.071 μs |  3.66 |    0.21 | 0.1221 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |             |            |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   8.275 μs |   1.4278 μs |  0.0783 μs |   8.254 μs |  1.00 |    0.01 | 0.0229 | 0.0153 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   6.824 μs |   8.6819 μs |  0.4759 μs |   6.554 μs |  0.82 |    0.05 | 0.0305 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  89.519 μs | 684.5020 μs | 37.5199 μs |  68.029 μs | 10.82 |    3.93 | 0.1221 |      - |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  33.774 μs |  35.5047 μs |  1.9461 μs |  32.670 μs |  4.08 |    0.21 | 0.1221 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |             |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **60.352 μs** |   **3.0132 μs** |  **1.7931 μs** |  **60.398 μs** |     **?** |       **?** | **0.2441** | **0.1831** |  **27.12 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  69.747 μs | 342.2517 μs | 18.7600 μs |  59.618 μs |     ? |       ? | 0.1831 | 0.1221 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |             |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **290.005 μs** |   **5.5023 μs** |  **2.8778 μs** | **290.088 μs** |     **?** |       **?** | **0.9766** | **0.4883** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 317.373 μs | 775.7846 μs | 42.5234 μs | 293.427 μs |     ? |       ? | 0.9766 | 0.4883 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |             |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **621.298 μs** |  **89.1161 μs** | **53.0315 μs** | **607.113 μs** |     **?** |       **?** | **1.9531** | **0.9766** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 600.754 μs | 351.0858 μs | 19.2442 μs | 596.308 μs |     ? |       ? | 1.9531 | 0.9766 | 174.78 KB |           ? |
