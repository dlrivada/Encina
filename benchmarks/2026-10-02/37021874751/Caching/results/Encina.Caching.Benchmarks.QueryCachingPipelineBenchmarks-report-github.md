```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean      | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |----------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |  **10.53 μs** |     **0.481 μs** |   **0.286 μs** |  **1.00** |    **0.04** | **0.0763** | **0.0610** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |  10.26 μs |     0.035 μs |   0.021 μs |  0.98 |    0.03 | 0.0916 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                | 106.00 μs |     0.729 μs |   0.381 μs | 10.08 |    0.26 | 1.2207 | 1.0986 |  37.49 KB |       18.97 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  48.36 μs |     0.117 μs |   0.077 μs |  4.60 |    0.12 | 0.4883 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |  10.20 μs |     2.787 μs |   0.153 μs |  1.00 |    0.02 | 0.0763 | 0.0610 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |  10.62 μs |     0.228 μs |   0.013 μs |  1.04 |    0.01 | 0.0916 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 116.77 μs |   495.717 μs |  27.172 μs | 11.45 |    2.31 | 0.7324 | 0.6104 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  50.92 μs |     1.073 μs |   0.059 μs |  5.00 |    0.07 | 0.4883 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **76.98 μs** |     **0.618 μs** |   **0.323 μs** |     **?** |       **?** | **0.6104** | **0.2441** |  **17.62 KB** |           **?** |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  85.92 μs |   274.139 μs |  15.026 μs |     ? |       ? | 0.6104 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **404.36 μs** |    **12.148 μs** |   **6.353 μs** |     **?** |       **?** | **3.4180** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 433.83 μs | 1,084.442 μs |  59.442 μs |     ? |       ? | 3.4180 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **804.78 μs** |    **20.739 μs** |  **10.847 μs** |     **?** |       **?** | **6.8359** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 852.50 μs | 1,935.980 μs | 106.118 μs |     ? |       ? | 6.8359 | 2.9297 | 174.78 KB |           ? |
