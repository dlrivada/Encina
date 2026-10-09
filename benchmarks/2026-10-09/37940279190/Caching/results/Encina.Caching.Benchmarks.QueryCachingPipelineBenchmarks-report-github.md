```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |  **10.015 μs** |     **0.5003 μs** |  **0.2977 μs** |  **1.00** |    **0.04** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   9.186 μs |     0.0540 μs |  0.0321 μs |  0.92 |    0.03 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                | 101.320 μs |     4.7674 μs |  2.8370 μs | 10.13 |    0.40 | 1.9531 | 1.3428 |  38.01 KB |       19.23 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  46.446 μs |     0.1862 μs |  0.1108 μs |  4.64 |    0.13 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.856 μs |     3.6525 μs |  0.2002 μs |  1.00 |    0.02 | 0.1678 | 0.1068 |   3.08 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   9.564 μs |     0.4206 μs |  0.0231 μs |  0.97 |    0.02 | 0.1526 |      - |   2.55 KB |        0.83 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 116.987 μs |   418.4112 μs | 22.9345 μs | 11.87 |    2.03 | 1.0986 | 0.4883 |  18.99 KB |        6.17 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  48.096 μs |     3.5618 μs |  0.1952 μs |  4.88 |    0.09 | 0.7324 |      - |  12.19 KB |        3.96 |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **79.665 μs** |     **3.2553 μs** |  **1.7026 μs** |     **?** |       **?** | **0.9766** | **0.2441** |  **17.62 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  82.802 μs |   221.1610 μs | 12.1226 μs |     ? |       ? | 0.9766 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **380.546 μs** |    **13.9456 μs** |  **7.2938 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 410.906 μs |   944.5083 μs | 51.7717 μs |     ? |       ? | 4.8828 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **787.110 μs** |    **29.2016 μs** | **15.2730 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 825.494 μs | 1,767.6067 μs | 96.8885 μs |     ? |       ? | 9.7656 | 2.9297 | 174.78 KB |           ? |
