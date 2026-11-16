```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **8.574 μs** |   **1.0910 μs** |  **0.6492 μs** |  **1.01** |    **0.10** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   6.124 μs |   0.0838 μs |  0.0438 μs |  0.72 |    0.05 |  0.2060 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  91.772 μs |   7.8038 μs |  4.6439 μs | 10.76 |    0.96 |  2.1973 | 1.4648 |  43.13 KB |       17.20 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  30.412 μs |   0.5107 μs |  0.3039 μs |  3.57 |    0.27 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   8.264 μs |   5.5594 μs |  0.3047 μs |  1.00 |    0.05 |  0.1526 | 0.0610 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   6.167 μs |   4.0280 μs |  0.2208 μs |  0.75 |    0.03 |  0.2060 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  90.448 μs | 446.2467 μs | 24.4603 μs | 10.95 |    2.59 |  1.4648 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  30.205 μs |   2.4949 μs |  0.1368 μs |  3.66 |    0.12 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **60.038 μs** |   **4.6753 μs** |  **2.7822 μs** |     **?** |       **?** |  **1.7700** | **0.8545** |  **31.87 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  66.243 μs | 216.1005 μs | 11.8452 μs |     ? |       ? |  1.3428 | 0.4272 |  22.39 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **278.882 μs** |  **13.4071 μs** |  **7.0122 μs** |     **?** |       **?** |  **6.3477** | **1.9531** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 310.377 μs | 831.9332 μs | 45.6011 μs |     ? |       ? |  6.3477 | 1.9531 |  111.3 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **562.508 μs** |  **51.9914 μs** | **27.1925 μs** |     **?** |       **?** | **12.6953** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 557.750 μs | 452.9115 μs | 24.8256 μs |     ? |       ? | 12.6953 | 3.9063 | 222.45 KB |           ? |
