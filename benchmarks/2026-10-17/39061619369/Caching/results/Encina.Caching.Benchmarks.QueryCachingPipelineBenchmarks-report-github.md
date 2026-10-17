```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev      | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|------------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.243 μs** |     **0.5123 μs** |   **0.3049 μs** |  **1.00** |    **0.04** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   8.559 μs |     0.0188 μs |   0.0124 μs |  0.93 |    0.03 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  92.838 μs |     4.7203 μs |   2.4688 μs | 10.05 |    0.41 |  2.1973 | 1.4648 |  43.01 KB |       17.15 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  42.783 μs |     0.1545 μs |   0.1022 μs |  4.63 |    0.15 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |             |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.024 μs |     4.8789 μs |   0.2674 μs |  1.00 |    0.04 |  0.1984 | 0.1068 |   3.61 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   8.568 μs |     0.2486 μs |   0.0136 μs |  0.95 |    0.02 |  0.1984 |      - |    3.4 KB |        0.94 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 106.625 μs |   500.0449 μs |  27.4091 μs | 11.82 |    2.65 |  1.4648 | 0.7324 |   24.3 KB |        6.73 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  42.625 μs |     2.5808 μs |   0.1415 μs |  4.73 |    0.12 |  0.9766 |      - |  16.41 KB |        4.54 |
|                                     |            |                |             |                  |            |               |             |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **69.130 μs** |     **1.8975 μs** |   **0.9924 μs** |     **?** |       **?** |  **1.3428** | **0.4883** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  76.060 μs |   195.5330 μs |  10.7178 μs |     ? |       ? |  1.3428 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **345.737 μs** |    **16.6405 μs** |   **8.7033 μs** |     **?** |       **?** |  **6.3477** | **1.9531** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 375.458 μs |   896.6139 μs |  49.1464 μs |     ? |       ? |  6.3477 | 1.9531 |  111.3 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **727.928 μs** |    **34.9408 μs** |  **18.2747 μs** |     **?** |       **?** | **12.6953** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 793.970 μs | 1,999.4106 μs | 109.5944 μs |     ? |       ? | 12.6953 | 3.9063 | 222.45 KB |           ? |
