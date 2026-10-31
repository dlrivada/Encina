```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **8.004 μs** |     **1.0432 μs** |  **0.6208 μs** |  **1.01** |    **0.10** |  **0.1526** | **0.0687** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   5.891 μs |     0.2185 μs |  0.1445 μs |  0.74 |    0.06 |  0.2060 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  73.609 μs |     4.4273 μs |  2.3156 μs |  9.24 |    0.72 |  2.1973 | 1.4648 |  43.28 KB |       17.26 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  28.078 μs |     0.4225 μs |  0.2514 μs |  3.53 |    0.25 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   7.556 μs |     3.4935 μs |  0.1915 μs |  1.00 |    0.03 |  0.1526 | 0.0687 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   5.652 μs |     1.3928 μs |  0.0763 μs |  0.75 |    0.02 |  0.2060 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  83.448 μs |   427.8676 μs | 23.4529 μs | 11.05 |    2.70 |  1.4648 | 0.6104 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  27.728 μs |     2.2513 μs |  0.1234 μs |  3.67 |    0.08 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **55.701 μs** |     **4.7791 μs** |  **2.8440 μs** |     **?** |       **?** |  **1.7700** | **0.9155** |  **31.85 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  61.495 μs |   233.7539 μs | 12.8128 μs |     ? |       ? |  1.3428 | 0.4272 |  22.39 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **290.127 μs** |    **45.2273 μs** | **26.9141 μs** |     **?** |       **?** |  **6.5918** | **2.1973** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 290.730 μs |   722.8602 μs | 39.6224 μs |     ? |       ? |  6.5918 | 2.1973 |  111.3 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **570.830 μs** |    **86.0321 μs** | **51.1963 μs** |     **?** |       **?** | **13.1836** | **4.3945** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 604.063 μs | 1,157.3585 μs | 63.4387 μs |     ? |       ? | 13.1836 | 4.3945 | 222.45 KB |           ? |
