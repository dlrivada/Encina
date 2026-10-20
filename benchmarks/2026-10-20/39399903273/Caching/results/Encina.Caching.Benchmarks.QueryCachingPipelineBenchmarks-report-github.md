```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |------------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |    **14.10 μs** |   **0.513 μs** |  **0.306 μs** |  **1.00** |    **0.03** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |    12.57 μs |   0.072 μs |  0.048 μs |  0.89 |    0.02 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |   133.80 μs |   4.071 μs |  2.129 μs |  9.49 |    0.24 |  1.4648 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |    61.73 μs |   0.247 μs |  0.129 μs |  4.38 |    0.09 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |    13.31 μs |   5.332 μs |  0.292 μs |  1.00 |    0.03 |  0.1984 | 0.1068 |   3.61 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |    12.72 μs |   0.507 μs |  0.028 μs |  0.96 |    0.02 |  0.1984 |      - |    3.4 KB |        0.94 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |   140.24 μs | 451.573 μs | 24.752 μs | 10.54 |    1.62 |  1.4648 | 0.4883 |   24.3 KB |        6.74 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |    65.11 μs |   3.721 μs |  0.204 μs |  4.89 |    0.09 |  0.9766 |      - |  16.41 KB |        4.55 |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |   **110.21 μs** |   **3.490 μs** |  **1.825 μs** |     **?** |       **?** |  **1.3428** | **0.3662** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |   114.02 μs | 228.245 μs | 12.511 μs |     ? |       ? |  1.3428 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               |   **522.40 μs** |  **20.092 μs** | **11.956 μs** |     **?** |       **?** |  **5.8594** | **1.9531** | **111.31 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               |   514.52 μs |  74.458 μs |  4.081 μs |     ? |       ? |  5.8594 | 1.9531 | 111.31 KB |           ? |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **1,007.74 μs** |  **30.977 μs** | **18.434 μs** |     **?** |       **?** | **11.7188** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 1,053.31 μs | 134.882 μs |  7.393 μs |     ? |       ? | 11.7188 | 3.9063 | 222.45 KB |           ? |
