```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |----------------- |------------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |    **14.95 μs** |  **0.463 μs** |  **0.276 μs** |  **1.00** |    **0.02** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | 3           | ?                |    12.95 μs |  0.245 μs |  0.162 μs |  0.87 |    0.02 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | 3           | ?                |   140.76 μs |  5.343 μs |  2.794 μs |  9.42 |    0.24 |  1.4648 | 0.4883 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | 3           | ?                |    66.52 μs |  2.614 μs |  1.555 μs |  4.45 |    0.13 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |             |                  |             |           |           |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | MediumRun  | 15             | 2           | 10          | ?                |    15.45 μs |  0.261 μs |  0.374 μs |  1.00 |    0.03 |  0.1526 | 0.0610 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | MediumRun  | 15             | 2           | 10          | ?                |    13.17 μs |  0.176 μs |  0.253 μs |  0.85 |    0.03 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | MediumRun  | 15             | 2           | 10          | ?                |   151.49 μs |  2.700 μs |  3.785 μs |  9.81 |    0.33 |  1.4648 | 0.4883 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | MediumRun  | 15             | 2           | 10          | ?                |    63.22 μs |  0.274 μs |  0.375 μs |  4.09 |    0.10 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |             |                  |             |           |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |   **107.92 μs** |  **1.994 μs** |  **1.043 μs** |     **?** |       **?** |  **1.3428** | **0.4883** |  **22.39 KB** |           **?** |
|                                     |            |                |             |             |                  |             |           |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 10               |   114.77 μs |  1.195 μs |  1.714 μs |     ? |       ? |  1.3428 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |             |                  |             |           |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               |   **511.52 μs** | **12.550 μs** |  **7.468 μs** |     **?** |       **?** |  **5.8594** | **1.9531** | **111.31 KB** |           **?** |
|                                     |            |                |             |             |                  |             |           |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 50               |   544.48 μs |  7.358 μs | 10.553 μs |     ? |       ? |  5.8594 | 1.9531 | 111.31 KB |           ? |
|                                     |            |                |             |             |                  |             |           |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **1,017.75 μs** | **41.573 μs** | **24.739 μs** |     **?** |       **?** | **11.7188** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |             |                  |             |           |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 100              | 1,081.19 μs | 15.793 μs | 22.650 μs |     ? |       ? | 11.7188 | 3.9063 | 222.45 KB |           ? |
