```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.133 μs** |   **0.5780 μs** |  **0.3439 μs** |  **1.00** |    **0.05** | **0.0229** | **0.0153** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   8.077 μs |   0.0634 μs |  0.0419 μs |  0.89 |    0.03 | 0.0305 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  74.754 μs |   5.2206 μs |  3.1067 μs |  8.20 |    0.44 | 0.3662 | 0.2441 |  37.87 KB |       19.16 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  41.047 μs |   0.5357 μs |  0.3188 μs |  4.50 |    0.17 | 0.1221 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   8.444 μs |   6.1486 μs |  0.3370 μs |  1.00 |    0.05 | 0.0229 | 0.0153 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   7.955 μs |   0.7586 μs |  0.0416 μs |  0.94 |    0.03 | 0.0305 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  88.694 μs | 502.0947 μs | 27.5215 μs | 10.51 |    2.85 | 0.1221 |      - |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  41.510 μs |   8.6481 μs |  0.4740 μs |  4.92 |    0.18 | 0.1221 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **66.393 μs** |   **2.5041 μs** |  **1.4902 μs** |     **?** |       **?** | **0.2441** | **0.1831** |  **27.06 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  73.882 μs | 266.6285 μs | 14.6148 μs |     ? |       ? | 0.1831 | 0.1221 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **323.864 μs** |   **6.9401 μs** |  **3.6298 μs** |     **?** |       **?** | **0.9766** | **0.4883** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 343.568 μs | 913.7184 μs | 50.0840 μs |     ? |       ? | 0.9766 | 0.4883 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **643.539 μs** |  **21.8761 μs** | **11.4416 μs** |     **?** |       **?** | **1.9531** | **0.9766** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 638.762 μs | 101.7179 μs |  5.5755 μs |     ? |       ? | 1.9531 | 0.9766 | 174.78 KB |           ? |
