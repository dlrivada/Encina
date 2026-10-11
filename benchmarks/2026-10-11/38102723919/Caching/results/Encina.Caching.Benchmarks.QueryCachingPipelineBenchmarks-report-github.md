```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |   **7.657 μs** |  **0.6747 μs** |  **0.4015 μs** |  **1.00** |    **0.07** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | 3           | ?                |   6.601 μs |  0.0144 μs |  0.0075 μs |  0.86 |    0.04 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | 3           | ?                |  81.549 μs |  5.3458 μs |  3.1812 μs | 10.68 |    0.66 | 1.9531 | 1.3428 |  38.01 KB |       19.23 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | 3           | ?                |  32.918 μs |  0.2465 μs |  0.1467 μs |  4.31 |    0.22 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |                  |            |            |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | MediumRun  | 15             | 2           | 10          | ?                |   8.283 μs |  0.3004 μs |  0.4308 μs |  1.00 |    0.07 | 0.1068 | 0.0458 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | MediumRun  | 15             | 2           | 10          | ?                |   6.625 μs |  0.0181 μs |  0.0248 μs |  0.80 |    0.04 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | MediumRun  | 15             | 2           | 10          | ?                |  82.985 μs |  3.5441 μs |  5.0828 μs | 10.04 |    0.78 | 1.0986 | 0.4883 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | MediumRun  | 15             | 2           | 10          | ?                |  31.977 μs |  0.1829 μs |  0.2624 μs |  3.87 |    0.19 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |                  |            |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **59.373 μs** |  **2.5914 μs** |  **1.3554 μs** |     **?** |       **?** | **1.4648** | **0.7324** |  **27.15 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 10               |  64.397 μs |  1.5407 μs |  2.2096 μs |     ? |       ? | 1.0376 | 0.3052 |  17.62 KB |           ? |
|                                     |            |                |             |             |                  |            |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **277.032 μs** |  **9.1659 μs** |  **4.7940 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 50               | 291.516 μs |  8.1766 μs | 11.7267 μs |     ? |       ? | 4.8828 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |             |                  |            |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **554.059 μs** | **23.0619 μs** | **12.0618 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 100              | 606.398 μs | 12.0933 μs | 16.9531 μs |     ? |       ? | 9.7656 | 2.9297 | 174.78 KB |           ? |
