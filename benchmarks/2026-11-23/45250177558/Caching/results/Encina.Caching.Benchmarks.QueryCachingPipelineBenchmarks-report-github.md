```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.40GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |------------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |    **13.05 μs** |   **0.406 μs** |  **0.242 μs** |  **1.00** |    **0.02** | **0.0916** | **0.0305** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |    14.68 μs |   0.050 μs |  0.033 μs |  1.13 |    0.02 | 0.1373 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |   137.93 μs |   8.456 μs |  4.423 μs | 10.57 |    0.37 | 0.9766 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |    71.28 μs |   0.336 μs |  0.222 μs |  5.46 |    0.10 | 0.6104 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |    12.89 μs |   1.237 μs |  0.068 μs |  1.00 |    0.01 | 0.0916 | 0.0305 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |    13.95 μs |   0.668 μs |  0.037 μs |  1.08 |    0.01 | 0.1373 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |   142.31 μs | 436.416 μs | 23.921 μs | 11.04 |    1.61 | 0.9766 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |    68.62 μs |   7.700 μs |  0.422 μs |  5.32 |    0.04 | 0.6104 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |   **108.36 μs** |   **1.858 μs** |  **0.972 μs** |     **?** |       **?** | **0.8545** | **0.3662** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |   110.11 μs | 272.870 μs | 14.957 μs |     ? |       ? | 0.8545 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               |   **516.87 μs** |   **9.040 μs** |  **4.728 μs** |     **?** |       **?** | **3.9063** | **0.9766** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               |   498.86 μs | 127.701 μs |  7.000 μs |     ? |       ? | 3.9063 | 0.9766 |  111.3 KB |           ? |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **1,053.06 μs** |  **21.691 μs** | **12.908 μs** |     **?** |       **?** | **7.8125** | **1.9531** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              |   996.68 μs | 164.731 μs |  9.029 μs |     ? |       ? | 7.8125 | 1.9531 | 222.45 KB |           ? |
