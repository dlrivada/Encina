```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |------------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |    **13.73 μs** |   **0.283 μs** |  **0.169 μs** |  **1.00** |    **0.02** | **0.0916** | **0.0305** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |    14.79 μs |   0.035 μs |  0.023 μs |  1.08 |    0.01 | 0.1373 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |   137.88 μs |   5.060 μs |  2.646 μs | 10.05 |    0.22 | 0.9766 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |    72.71 μs |   0.325 μs |  0.193 μs |  5.30 |    0.06 | 0.6104 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |    13.04 μs |   2.232 μs |  0.122 μs |  1.00 |    0.01 | 0.0916 | 0.0305 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |    14.12 μs |   0.415 μs |  0.023 μs |  1.08 |    0.01 | 0.1373 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |   145.71 μs | 456.776 μs | 25.037 μs | 11.18 |    1.67 | 0.9766 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |    67.65 μs |   4.748 μs |  0.260 μs |  5.19 |    0.05 | 0.6104 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |   **108.45 μs** |   **1.936 μs** |  **1.012 μs** |     **?** |       **?** | **0.8545** | **0.3662** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |   112.71 μs | 249.506 μs | 13.676 μs |     ? |       ? | 0.8545 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               |   **523.37 μs** |   **4.728 μs** |  **2.473 μs** |     **?** |       **?** | **3.9063** | **0.9766** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               |   519.11 μs | 126.108 μs |  6.912 μs |     ? |       ? | 3.9063 | 0.9766 |  111.3 KB |           ? |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **1,057.48 μs** |  **24.069 μs** | **14.323 μs** |     **?** |       **?** | **7.8125** | **1.9531** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 1,023.27 μs |  94.856 μs |  5.199 μs |     ? |       ? | 7.8125 | 1.9531 | 222.45 KB |           ? |
