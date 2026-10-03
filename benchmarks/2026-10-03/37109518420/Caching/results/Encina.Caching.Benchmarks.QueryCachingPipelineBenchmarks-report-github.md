```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.516 μs** |     **0.2925 μs** |   **0.1741 μs** |  **1.00** |    **0.02** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   9.454 μs |     0.0492 μs |   0.0258 μs |  0.99 |    0.02 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  95.260 μs |     2.9838 μs |   1.5606 μs | 10.01 |    0.23 | 1.9531 | 1.3428 |  37.85 KB |       19.15 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  47.560 μs |     0.1541 μs |   0.1019 μs |  5.00 |    0.09 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.412 μs |     1.1802 μs |   0.0647 μs |  1.00 |    0.01 | 0.1526 | 0.0916 |   3.07 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   9.253 μs |     0.6212 μs |   0.0341 μs |  0.98 |    0.01 | 0.1526 |      - |   2.55 KB |        0.83 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 111.790 μs |   440.5034 μs |  24.1455 μs | 11.88 |    2.22 | 1.0986 | 0.4883 |  18.99 KB |        6.19 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  47.078 μs |     3.1071 μs |   0.1703 μs |  5.00 |    0.03 | 0.7324 |      - |  12.19 KB |        3.97 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **74.256 μs** |     **1.6517 μs** |   **0.8639 μs** |     **?** |       **?** | **0.9766** | **0.3662** |  **17.62 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  79.105 μs |   221.7941 μs |  12.1573 μs |     ? |       ? | 0.9766 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **357.775 μs** |    **15.8500 μs** |   **8.2898 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 388.379 μs |   866.3524 μs |  47.4877 μs |     ? |       ? | 4.8828 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **734.067 μs** |    **13.9778 μs** |   **7.3107 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 783.518 μs | 1,872.1166 μs | 102.6170 μs |     ? |       ? | 9.7656 | 2.9297 | 174.78 KB |           ? |
