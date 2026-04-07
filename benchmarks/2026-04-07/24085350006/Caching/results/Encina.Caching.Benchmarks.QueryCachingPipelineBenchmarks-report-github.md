```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.689 μs** |   **0.2169 μs** |  **0.1135 μs** |  **1.00** |    **0.02** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   9.258 μs |   0.0344 μs |  0.0228 μs |  0.96 |    0.01 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  99.091 μs |   3.5584 μs |  2.1175 μs | 10.23 |    0.24 | 1.9531 | 1.3428 |  37.89 KB |       19.17 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  47.701 μs |   0.2807 μs |  0.1671 μs |  4.92 |    0.06 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.782 μs |   7.1383 μs |  0.3913 μs |  1.00 |    0.05 | 0.1068 | 0.0458 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   9.501 μs |   5.8394 μs |  0.3201 μs |  0.97 |    0.04 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 112.190 μs | 431.2960 μs | 23.6408 μs | 11.48 |    2.13 | 1.0986 | 0.4883 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  48.890 μs |   4.7487 μs |  0.2603 μs |  5.00 |    0.17 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **76.185 μs** |   **2.0533 μs** |  **1.0739 μs** |     **?** |       **?** | **0.9766** | **0.3662** |  **17.62 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  80.738 μs | 199.3615 μs | 10.9277 μs |     ? |       ? | 0.9766 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **383.182 μs** |   **4.6330 μs** |  **2.4232 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 375.851 μs |  61.8115 μs |  3.3881 μs |     ? |       ? | 4.8828 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **758.645 μs** |   **7.5600 μs** |  **3.9540 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 763.206 μs | 147.2403 μs |  8.0707 μs |     ? |       ? | 9.7656 | 2.9297 | 174.78 KB |           ? |
