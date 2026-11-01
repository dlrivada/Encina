```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |----------------- |-----------:|-----------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |  **10.478 μs** |  **0.9887 μs** |  **0.5884 μs** |  **1.00** |    **0.08** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | 3           | ?                |   8.593 μs |  0.0239 μs |  0.0142 μs |  0.82 |    0.04 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | 3           | ?                | 109.833 μs |  9.6373 μs |  5.7350 μs | 10.51 |    0.76 |  2.1973 | 1.4648 |  43.18 KB |       17.22 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | 3           | ?                |  42.249 μs |  0.1148 μs |  0.0683 μs |  4.04 |    0.21 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |             |                  |            |            |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | MediumRun  | 15             | 2           | 10          | ?                |  10.932 μs |  0.4725 μs |  0.6467 μs |  1.00 |    0.08 |  0.1526 | 0.0610 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | MediumRun  | 15             | 2           | 10          | ?                |   8.501 μs |  0.0289 μs |  0.0414 μs |  0.78 |    0.04 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | MediumRun  | 15             | 2           | 10          | ?                | 108.410 μs |  3.4929 μs |  5.0093 μs |  9.95 |    0.72 |  1.4648 | 0.6104 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | MediumRun  | 15             | 2           | 10          | ?                |  42.526 μs |  0.1818 μs |  0.2666 μs |  3.90 |    0.22 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |             |                  |            |            |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **71.437 μs** |  **1.7107 μs** |  **0.8948 μs** |     **?** |       **?** |  **1.3428** | **0.4883** |  **22.39 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 10               |  75.662 μs |  1.6511 μs |  2.3680 μs |     ? |       ? |  1.3428 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |             |                  |            |            |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **355.588 μs** | **19.9440 μs** | **10.4311 μs** |     **?** |       **?** |  **6.3477** | **1.9531** |  **111.3 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 50               | 368.876 μs |  9.1232 μs | 12.4879 μs |     ? |       ? |  6.3477 | 1.9531 | 111.31 KB |           ? |
|                                     |            |                |             |             |                  |            |            |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **721.318 μs** | **33.1807 μs** | **17.3542 μs** |     **?** |       **?** | **12.6953** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 100              | 744.517 μs | 14.8366 μs | 19.8064 μs |     ? |       ? | 12.6953 | 3.9063 | 222.47 KB |           ? |
