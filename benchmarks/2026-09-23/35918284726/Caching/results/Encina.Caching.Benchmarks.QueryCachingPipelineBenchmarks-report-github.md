```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.860 μs** |   **0.4273 μs** |  **0.2235 μs** |  **1.00** |    **0.03** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   9.498 μs |   0.0724 μs |  0.0479 μs |  0.96 |    0.02 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                | 100.124 μs |   4.2921 μs |  2.5542 μs | 10.16 |    0.33 | 1.9531 | 1.3428 |  37.85 KB |       19.15 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  47.796 μs |   0.1289 μs |  0.0853 μs |  4.85 |    0.11 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.982 μs |   1.8601 μs |  0.1020 μs |  1.00 |    0.01 | 0.1068 | 0.0458 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   9.198 μs |   0.2755 μs |  0.0151 μs |  0.92 |    0.01 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 110.299 μs | 474.9088 μs | 26.0314 μs | 11.05 |    2.26 | 1.0986 | 0.4883 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  47.679 μs |   2.0453 μs |  0.1121 μs |  4.78 |    0.04 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **75.232 μs** |   **2.0840 μs** |  **1.0900 μs** |     **?** |       **?** | **0.9766** | **0.2441** |  **17.62 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  85.266 μs | 222.4151 μs | 12.1913 μs |     ? |       ? | 0.9766 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **378.575 μs** |  **14.7198 μs** |  **7.6988 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 410.073 μs | 888.6660 μs | 48.7108 μs |     ? |       ? | 4.8828 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **748.562 μs** |  **19.0397 μs** |  **9.9581 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 753.792 μs | 137.8790 μs |  7.5576 μs |     ? |       ? | 9.7656 | 2.9297 | 174.78 KB |           ? |
