```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |  **13.11 μs** |   **0.603 μs** |  **0.359 μs** |  **1.00** |    **0.04** |  **0.1526** | **0.0763** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |  13.10 μs |   0.054 μs |  0.035 μs |  1.00 |    0.03 |  0.1984 |      - |    3.4 KB |        1.35 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                | 128.33 μs |   3.868 μs |  2.023 μs |  9.79 |    0.29 |  1.4648 | 0.4883 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  63.56 μs |   0.342 μs |  0.226 μs |  4.85 |    0.13 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |  13.24 μs |   2.746 μs |  0.150 μs |  1.00 |    0.01 |  0.1984 | 0.1068 |   3.61 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |  12.51 μs |   0.245 μs |  0.013 μs |  0.94 |    0.01 |  0.1984 |      - |    3.4 KB |        0.94 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 142.33 μs | 357.479 μs | 19.595 μs | 10.75 |    1.29 |  1.4648 | 0.4883 |   24.3 KB |        6.73 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  64.22 μs |   4.243 μs |  0.233 μs |  4.85 |    0.05 |  0.9766 |      - |  16.41 KB |        4.54 |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **99.40 μs** |   **2.750 μs** |  **1.438 μs** |     **?** |       **?** |  **1.3428** | **0.4883** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               | 107.59 μs | 202.765 μs | 11.114 μs |     ? |       ? |  1.3428 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **486.88 μs** |  **12.235 μs** |  **7.281 μs** |     **?** |       **?** |  **5.8594** | **1.9531** | **111.31 KB** |           **?** |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 474.77 μs | 148.709 μs |  8.151 μs |     ? |       ? |  5.8594 | 1.9531 | 111.31 KB |           ? |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **985.47 μs** |  **34.111 μs** | **20.299 μs** |     **?** |       **?** | **11.7188** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 964.56 μs |  80.864 μs |  4.432 μs |     ? |       ? | 11.7188 | 3.9063 | 222.45 KB |           ? |
