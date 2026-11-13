```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.43GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **8.656 μs** |     **0.9921 μs** |  **0.5904 μs** |  **1.00** |    **0.09** |  **0.1526** | **0.0687** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   5.961 μs |     0.1214 μs |  0.0803 μs |  0.69 |    0.05 |  0.2060 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  77.341 μs |     6.1432 μs |  3.6557 μs |  8.97 |    0.71 |  2.1973 | 1.4648 |  43.33 KB |       17.28 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  29.364 μs |     0.8834 μs |  0.5843 μs |  3.41 |    0.23 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   8.275 μs |    13.0791 μs |  0.7169 μs |  1.00 |    0.10 |  0.1526 | 0.0610 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   6.052 μs |     3.8682 μs |  0.2120 μs |  0.73 |    0.06 |  0.2060 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  93.778 μs |   439.1432 μs | 24.0709 μs | 11.39 |    2.67 |  1.4648 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  28.212 μs |     8.1188 μs |  0.4450 μs |  3.43 |    0.25 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **58.222 μs** |     **4.0192 μs** |  **2.3918 μs** |     **?** |       **?** |  **1.7700** | **0.8545** |  **31.83 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  63.690 μs |   249.8162 μs | 13.6933 μs |     ? |       ? |  1.3428 | 0.4272 |  22.39 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **275.933 μs** |    **24.7701 μs** | **12.9553 μs** |     **?** |       **?** |  **6.3477** | **1.9531** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 293.128 μs | 1,101.4964 μs | 60.3767 μs |     ? |       ? |  6.3477 | 1.9531 |  111.3 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **608.073 μs** |    **86.7609 μs** | **51.6300 μs** |     **?** |       **?** | **13.1836** | **4.3945** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 527.908 μs |   310.4466 μs | 17.0166 μs |     ? |       ? | 12.6953 | 3.9063 | 222.45 KB |           ? |
