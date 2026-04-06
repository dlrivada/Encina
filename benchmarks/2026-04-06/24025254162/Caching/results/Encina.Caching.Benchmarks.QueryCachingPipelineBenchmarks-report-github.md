```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.569 μs** |     **0.3458 μs** |  **0.2058 μs** |  **1.00** |    **0.03** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   9.589 μs |     0.0757 μs |  0.0451 μs |  1.00 |    0.02 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  96.573 μs |     2.7094 μs |  1.4171 μs | 10.10 |    0.25 | 1.9531 | 1.4648 |   37.9 KB |       19.18 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  48.297 μs |     0.1464 μs |  0.0968 μs |  5.05 |    0.11 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.362 μs |     1.9626 μs |  0.1076 μs |  1.00 |    0.01 | 0.1068 | 0.0458 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   9.477 μs |     1.0117 μs |  0.0555 μs |  1.01 |    0.01 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 110.384 μs |   454.9219 μs | 24.9358 μs | 11.79 |    2.31 | 1.0986 | 0.4883 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  45.802 μs |     0.9598 μs |  0.0526 μs |  4.89 |    0.05 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **73.949 μs** |     **1.1389 μs** |  **0.5956 μs** |     **?** |       **?** | **0.9766** | **0.3662** |  **17.62 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  82.937 μs |   223.8822 μs | 12.2717 μs |     ? |       ? | 0.9766 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **378.064 μs** |    **15.4808 μs** |  **8.0968 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 391.672 μs |   833.9206 μs | 45.7100 μs |     ? |       ? | 4.8828 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **752.051 μs** |    **21.2703 μs** | **11.1248 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 766.892 μs | 1,718.2562 μs | 94.1834 μs |     ? |       ? | 9.7656 | 2.9297 | 174.78 KB |           ? |
