```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.432 μs** |     **0.3471 μs** |  **0.2066 μs** |  **1.00** |    **0.03** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   9.244 μs |     0.0229 μs |  0.0120 μs |  0.98 |    0.02 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  94.931 μs |     3.2550 μs |  1.7024 μs | 10.07 |    0.27 | 1.9531 | 1.3428 |  37.97 KB |       19.21 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  46.891 μs |     0.1271 μs |  0.0841 μs |  4.97 |    0.10 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.209 μs |     3.6615 μs |  0.2007 μs |  1.00 |    0.03 | 0.1068 | 0.0458 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   9.288 μs |     0.1187 μs |  0.0065 μs |  1.01 |    0.02 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 112.344 μs |   358.6925 μs | 19.6611 μs | 12.20 |    1.86 | 1.0986 | 0.4883 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  47.318 μs |     4.3972 μs |  0.2410 μs |  5.14 |    0.10 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **77.226 μs** |     **2.1029 μs** |  **1.0998 μs** |     **?** |       **?** | **0.9766** | **0.3662** |  **17.62 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  80.796 μs |   212.4899 μs | 11.6473 μs |     ? |       ? | 0.9766 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **386.717 μs** |     **7.3972 μs** |  **3.8689 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 406.803 μs |   986.8866 μs | 54.0946 μs |     ? |       ? | 4.8828 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **743.664 μs** |    **13.0588 μs** |  **6.8300 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 808.873 μs | 1,773.3000 μs | 97.2006 μs |     ? |       ? | 9.7656 | 2.9297 | 174.78 KB |           ? |
