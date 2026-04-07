```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean       | Error      | StdDev     | Median     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |----------------- |-----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |   **9.193 μs** |  **0.4744 μs** |  **0.2823 μs** |   **9.093 μs** |  **1.00** |    **0.04** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | 3           | ?                |   8.369 μs |  0.0458 μs |  0.0273 μs |   8.358 μs |  0.91 |    0.03 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | 3           | ?                |  92.567 μs |  3.0661 μs |  1.6036 μs |  93.398 μs | 10.08 |    0.33 | 1.9531 | 1.3428 |  37.87 KB |       19.16 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | 3           | ?                |  40.431 μs |  0.1946 μs |  0.1158 μs |  40.394 μs |  4.40 |    0.13 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | MediumRun  | 15             | 2           | 10          | ?                |   9.545 μs |  0.2887 μs |  0.4047 μs |   9.419 μs |  1.00 |    0.06 | 0.1068 | 0.0458 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | MediumRun  | 15             | 2           | 10          | ?                |   8.378 μs |  0.1459 μs |  0.2046 μs |   8.232 μs |  0.88 |    0.04 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | MediumRun  | 15             | 2           | 10          | ?                |  97.869 μs |  2.3495 μs |  3.1365 μs |  97.843 μs | 10.27 |    0.52 | 1.0986 | 0.4883 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | MediumRun  | 15             | 2           | 10          | ?                |  41.074 μs |  0.3270 μs |  0.4584 μs |  40.789 μs |  4.31 |    0.18 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **67.966 μs** |  **3.0446 μs** |  **1.5924 μs** |  **67.759 μs** |     **?** |       **?** | **0.9766** | **0.3662** |  **17.62 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 10               |  70.562 μs |  0.8950 μs |  1.1949 μs |  70.503 μs |     ? |       ? | 0.9766 | 0.3662 |  17.62 KB |           ? |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **345.571 μs** | **22.5984 μs** | **11.8194 μs** | **343.527 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 50               | 347.348 μs |  7.2700 μs | 10.4265 μs | 342.973 μs |     ? |       ? | 4.8828 | 1.4648 |  87.48 KB |           ? |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **678.958 μs** | **32.7570 μs** | **17.1325 μs** | **674.348 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 100              | 693.814 μs | 10.7622 μs | 15.0871 μs | 687.846 μs |     ? |       ? | 9.7656 | 2.9297 |  174.8 KB |           ? |
