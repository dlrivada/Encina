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
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.856 μs** |     **0.2100 μs** |  **0.1098 μs** |  **1.00** |    **0.01** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   9.083 μs |     0.0762 μs |  0.0504 μs |  0.92 |    0.01 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  99.011 μs |     3.8193 μs |  2.2728 μs | 10.05 |    0.24 | 1.9531 | 1.3428 |  37.83 KB |       19.14 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  45.828 μs |     0.1414 μs |  0.0936 μs |  4.65 |    0.05 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.349 μs |     3.0548 μs |  0.1674 μs |  1.00 |    0.02 | 0.1526 | 0.0916 |   3.06 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   9.253 μs |     1.8129 μs |  0.0994 μs |  0.99 |    0.02 | 0.1526 |      - |   2.55 KB |        0.83 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 112.881 μs |   452.8603 μs | 24.8228 μs | 12.08 |    2.31 | 1.0986 | 0.4883 |  18.99 KB |        6.20 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  46.326 μs |     1.8960 μs |  0.1039 μs |  4.96 |    0.08 | 0.7324 |      - |  12.19 KB |        3.98 |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **74.871 μs** |     **2.1507 μs** |  **1.1249 μs** |     **?** |       **?** | **0.9766** | **0.3662** |  **17.62 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  79.577 μs |   216.0136 μs | 11.8404 μs |     ? |       ? | 0.9766 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **384.244 μs** |    **16.6064 μs** |  **8.6855 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 401.719 μs |   907.6500 μs | 49.7514 μs |     ? |       ? | 4.8828 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **756.740 μs** |    **32.8950 μs** | **17.2047 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 789.384 μs | 1,768.8512 μs | 96.9567 μs |     ? |       ? | 9.7656 | 2.9297 | 174.78 KB |           ? |
