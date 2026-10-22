```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |----------------- |------------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |    **13.66 μs** |  **0.353 μs** |  **0.210 μs** |  **1.00** |    **0.02** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | 3           | ?                |    13.08 μs |  0.056 μs |  0.033 μs |  0.96 |    0.01 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | 3           | ?                |   134.16 μs |  6.191 μs |  3.238 μs |  9.82 |    0.27 |  1.4648 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | 3           | ?                |    63.34 μs |  0.318 μs |  0.210 μs |  4.64 |    0.07 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |             |                  |             |           |           |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | MediumRun  | 15             | 2           | 10          | ?                |    13.86 μs |  0.424 μs |  0.608 μs |  1.00 |    0.06 |  0.1526 | 0.0610 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | MediumRun  | 15             | 2           | 10          | ?                |    12.40 μs |  0.093 μs |  0.133 μs |  0.90 |    0.04 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | MediumRun  | 15             | 2           | 10          | ?                |   138.54 μs |  2.004 μs |  2.809 μs | 10.02 |    0.46 |  1.4648 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | MediumRun  | 15             | 2           | 10          | ?                |    63.23 μs |  0.560 μs |  0.803 μs |  4.57 |    0.20 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |             |                  |             |           |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |   **106.59 μs** |  **2.932 μs** |  **1.534 μs** |     **?** |       **?** |  **1.3428** | **0.4883** |  **22.39 KB** |           **?** |
|                                     |            |                |             |             |                  |             |           |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 10               |   105.10 μs |  0.895 μs |  1.283 μs |     ? |       ? |  1.3428 | 0.4883 |  22.39 KB |           ? |
|                                     |            |                |             |             |                  |             |           |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               |   **494.37 μs** | **12.886 μs** |  **7.668 μs** |     **?** |       **?** |  **5.8594** | **1.9531** | **111.31 KB** |           **?** |
|                                     |            |                |             |             |                  |             |           |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 50               |   516.00 μs |  6.078 μs |  8.320 μs |     ? |       ? |  6.3477 | 1.9531 |  111.3 KB |           ? |
|                                     |            |                |             |             |                  |             |           |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              |   **984.54 μs** | **31.709 μs** | **18.870 μs** |     **?** |       **?** | **11.7188** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |             |                  |             |           |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 100              | 1,015.59 μs | 15.152 μs | 21.241 μs |     ? |       ? | 11.7188 | 3.9063 | 222.45 KB |           ? |
