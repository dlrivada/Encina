```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3


```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean       | Error      | StdDev     | Median     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |----------------- |-----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |   **9.026 μs** |  **0.5576 μs** |  **0.3318 μs** |   **9.009 μs** |  **1.00** |    **0.05** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | 3           | ?                |   8.035 μs |  0.0178 μs |  0.0106 μs |   8.036 μs |  0.89 |    0.03 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | 3           | ?                |  93.366 μs |  9.8180 μs |  5.8425 μs |  91.643 μs | 10.36 |    0.71 | 1.9531 | 1.4648 |   37.9 KB |       19.17 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | 3           | ?                |  39.785 μs |  0.0842 μs |  0.0557 μs |  39.801 μs |  4.41 |    0.15 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | MediumRun  | 15             | 2           | 10          | ?                |  10.334 μs |  0.3708 μs |  0.5318 μs |  10.220 μs |  1.00 |    0.07 | 0.1068 | 0.0458 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | MediumRun  | 15             | 2           | 10          | ?                |   8.437 μs |  0.2203 μs |  0.3088 μs |   8.707 μs |  0.82 |    0.05 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | MediumRun  | 15             | 2           | 10          | ?                | 107.677 μs |  3.1925 μs |  4.5786 μs | 107.283 μs | 10.45 |    0.68 | 1.0986 | 0.4883 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | MediumRun  | 15             | 2           | 10          | ?                |  40.278 μs |  0.6349 μs |  0.8901 μs |  40.996 μs |  3.91 |    0.21 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **72.036 μs** |  **3.4219 μs** |  **1.7897 μs** |  **72.478 μs** |     **?** |       **?** | **0.9766** | **0.2441** |  **17.62 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 10               |  74.119 μs |  0.8163 μs |  1.1444 μs |  73.887 μs |     ? |       ? | 0.9766 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **360.203 μs** | **16.0814 μs** |  **8.4109 μs** | **360.164 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 50               | 355.913 μs |  6.9718 μs |  9.7735 μs | 352.762 μs |     ? |       ? | 4.8828 | 1.4648 |  87.48 KB |           ? |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **662.709 μs** | **14.0960 μs** |  **7.3725 μs** | **661.788 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 100              | 689.062 μs |  9.3356 μs | 12.4627 μs | 686.677 μs |     ? |       ? | 9.7656 | 2.9297 |  174.8 KB |           ? |
