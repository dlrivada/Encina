```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |------------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |    **13.69 μs** |   **0.492 μs** |  **0.293 μs** |  **1.00** |    **0.03** | **0.0916** | **0.0305** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |    14.48 μs |   0.036 μs |  0.024 μs |  1.06 |    0.02 | 0.1373 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |   137.78 μs |   5.153 μs |  2.695 μs | 10.07 |    0.27 | 0.9766 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |    70.51 μs |   0.333 μs |  0.174 μs |  5.15 |    0.10 | 0.6104 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |    13.49 μs |   2.266 μs |  0.124 μs |  1.00 |    0.01 | 0.0916 | 0.0305 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |    14.10 μs |   0.359 μs |  0.020 μs |  1.05 |    0.01 | 0.1373 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |   150.39 μs | 531.611 μs | 29.139 μs | 11.15 |    1.87 | 0.9766 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |    69.45 μs |   5.371 μs |  0.294 μs |  5.15 |    0.05 | 0.6104 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |   **110.28 μs** |   **1.831 μs** |  **0.957 μs** |     **?** |       **?** | **0.8545** | **0.3662** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |   115.23 μs | 224.478 μs | 12.304 μs |     ? |       ? | 0.8545 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               |   **546.96 μs** |  **12.738 μs** |  **7.580 μs** |     **?** |       **?** | **3.9063** | **0.9766** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               |   510.38 μs | 116.648 μs |  6.394 μs |     ? |       ? | 3.9063 | 0.9766 |  111.3 KB |           ? |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **1,080.17 μs** |  **18.966 μs** | **11.287 μs** |     **?** |       **?** | **7.8125** | **1.9531** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 1,044.62 μs | 150.254 μs |  8.236 μs |     ? |       ? | 7.8125 | 1.9531 | 222.45 KB |           ? |
