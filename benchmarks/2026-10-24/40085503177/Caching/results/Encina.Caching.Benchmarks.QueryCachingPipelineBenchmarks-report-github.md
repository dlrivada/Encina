```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **8.212 μs** |     **1.0815 μs** |  **0.6436 μs** |  **1.01** |    **0.10** |  **0.1526** | **0.0687** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   5.889 μs |     0.1572 μs |  0.1040 μs |  0.72 |    0.05 |  0.2060 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  77.383 μs |     5.2049 μs |  3.0974 μs |  9.47 |    0.77 |  2.1973 | 1.4648 |  43.16 KB |       17.21 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  27.752 μs |     0.3597 μs |  0.2140 μs |  3.40 |    0.25 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   7.117 μs |     4.8177 μs |  0.2641 μs |  1.00 |    0.05 |  0.1526 | 0.0687 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   5.794 μs |     0.6684 μs |  0.0366 μs |  0.81 |    0.03 |  0.2060 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  83.743 μs |   407.1167 μs | 22.3154 μs | 11.78 |    2.74 |  1.4648 | 0.6104 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  27.690 μs |     2.2685 μs |  0.1243 μs |  3.89 |    0.12 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **55.071 μs** |     **4.4598 μs** |  **2.6539 μs** |     **?** |       **?** |  **1.7700** | **0.8545** |  **31.88 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  57.138 μs |   187.2081 μs | 10.2615 μs |     ? |       ? |  1.3428 | 0.4272 |  22.39 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **284.902 μs** |    **39.9780 μs** | **23.7903 μs** |     **?** |       **?** |  **6.5918** | **2.1973** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 274.704 μs |   724.2488 μs | 39.6985 μs |     ? |       ? |  6.3477 | 1.9531 |  111.3 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **545.484 μs** |    **40.4168 μs** | **21.1387 μs** |     **?** |       **?** | **13.1836** | **4.3945** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 577.452 μs | 1,331.6801 μs | 72.9939 μs |     ? |       ? | 13.1836 | 4.3945 | 222.45 KB |           ? |
