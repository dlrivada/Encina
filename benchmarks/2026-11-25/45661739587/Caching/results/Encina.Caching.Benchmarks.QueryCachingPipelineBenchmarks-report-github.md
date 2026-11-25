```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.787 μs** |   **0.5025 μs** |  **0.2990 μs** |  **1.00** |    **0.04** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   8.308 μs |   0.0760 μs |  0.0397 μs |  0.85 |    0.02 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  98.500 μs |   5.3834 μs |  3.2036 μs | 10.07 |    0.43 |  2.1973 | 1.3428 |  43.22 KB |       17.23 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  42.871 μs |   0.6948 μs |  0.4595 μs |  4.38 |    0.13 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.560 μs |   1.3789 μs |  0.0756 μs |  1.00 |    0.01 |  0.1526 | 0.0610 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   8.503 μs |   0.4710 μs |  0.0258 μs |  0.89 |    0.01 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 108.467 μs | 485.5551 μs | 26.6149 μs | 11.35 |    2.41 |  1.4648 | 0.6104 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  42.714 μs |   6.8471 μs |  0.3753 μs |  4.47 |    0.05 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **71.375 μs** |   **3.8976 μs** |  **2.0385 μs** |     **?** |       **?** |  **1.3428** | **0.4883** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  75.904 μs | 205.0017 μs | 11.2368 μs |     ? |       ? |  1.3428 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **360.083 μs** |  **24.0437 μs** | **14.3080 μs** |     **?** |       **?** |  **6.3477** | **1.9531** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 383.807 μs | 867.3398 μs | 47.5418 μs |     ? |       ? |  6.3477 | 1.9531 |  111.3 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **708.418 μs** |  **24.4984 μs** | **12.8131 μs** |     **?** |       **?** | **12.6953** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 688.355 μs | 105.5526 μs |  5.7857 μs |     ? |       ? | 12.6953 | 3.9063 | 222.45 KB |           ? |
