```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |  **13.36 μs** |   **0.553 μs** |  **0.329 μs** |  **1.00** |    **0.03** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |  12.50 μs |   0.031 μs |  0.019 μs |  0.94 |    0.02 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                | 128.52 μs |   6.861 μs |  3.588 μs |  9.62 |    0.34 |  1.4648 | 0.4883 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  63.51 μs |   0.579 μs |  0.383 μs |  4.76 |    0.12 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |  12.46 μs |   3.207 μs |  0.176 μs |  1.00 |    0.02 |  0.1984 | 0.1068 |   3.61 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |  12.53 μs |   0.648 μs |  0.035 μs |  1.01 |    0.01 |  0.1984 |      - |    3.4 KB |        0.94 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 137.68 μs | 396.657 μs | 21.742 μs | 11.05 |    1.52 |  1.4648 | 0.4883 |   24.3 KB |        6.74 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  64.37 μs |   5.711 μs |  0.313 μs |  5.17 |    0.07 |  0.9766 |      - |  16.41 KB |        4.55 |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **100.99 μs** |   **2.537 μs** |  **1.327 μs** |     **?** |       **?** |  **1.3428** | **0.3662** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               | 107.51 μs | 180.292 μs |  9.882 μs |     ? |       ? |  1.3428 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **477.61 μs** |  **10.590 μs** |  **6.302 μs** |     **?** |       **?** |  **5.8594** | **1.9531** | **111.31 KB** |           **?** |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 474.03 μs |  14.134 μs |  0.775 μs |     ? |       ? |  5.8594 | 1.9531 | 111.31 KB |           ? |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **998.70 μs** |  **17.744 μs** | **10.559 μs** |     **?** |       **?** | **11.7188** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 979.93 μs | 129.479 μs |  7.097 μs |     ? |       ? | 11.7188 | 3.9063 | 222.45 KB |           ? |
