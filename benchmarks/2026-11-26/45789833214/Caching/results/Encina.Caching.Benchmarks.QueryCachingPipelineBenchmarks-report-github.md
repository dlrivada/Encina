```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |------------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |    **13.54 μs** |   **0.391 μs** |  **0.232 μs** |  **1.00** |    **0.02** |  **0.1526** | **0.0763** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |    13.00 μs |   0.076 μs |  0.045 μs |  0.96 |    0.02 |  0.1984 |      - |    3.4 KB |        1.35 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |   141.04 μs |   3.748 μs |  1.960 μs | 10.42 |    0.22 |  1.4648 | 0.4883 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |    65.58 μs |   0.747 μs |  0.494 μs |  4.85 |    0.09 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |    13.05 μs |   4.374 μs |  0.240 μs |  1.00 |    0.02 |  0.1984 | 0.1068 |   3.59 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |    13.22 μs |   0.977 μs |  0.054 μs |  1.01 |    0.02 |  0.1984 |      - |    3.4 KB |        0.95 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |   138.32 μs | 414.857 μs | 22.740 μs | 10.60 |    1.52 |  1.4648 | 0.4883 |   24.3 KB |        6.77 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |    65.52 μs |   3.528 μs |  0.193 μs |  5.02 |    0.08 |  0.9766 |      - |  16.41 KB |        4.57 |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |   **105.68 μs** |   **2.343 μs** |  **1.225 μs** |     **?** |       **?** |  **1.3428** | **0.4883** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |   109.29 μs | 197.083 μs | 10.803 μs |     ? |       ? |  1.3428 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               |   **493.19 μs** |  **18.472 μs** | **10.993 μs** |     **?** |       **?** |  **5.8594** | **1.9531** | **111.31 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               |   493.95 μs |  48.380 μs |  2.652 μs |     ? |       ? |  5.8594 | 1.9531 | 111.31 KB |           ? |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              |   **982.19 μs** |  **26.001 μs** | **15.473 μs** |     **?** |       **?** | **11.7188** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 1,022.69 μs |  18.661 μs |  1.023 μs |     ? |       ? | 11.7188 | 3.9063 | 222.45 KB |           ? |
