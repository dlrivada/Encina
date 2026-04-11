```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |----------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |  **10.47 μs** |  **0.431 μs** |  **0.257 μs** |  **1.00** |    **0.03** | **0.0763** | **0.0610** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | 3           | ?                |  10.50 μs |  0.042 μs |  0.028 μs |  1.00 |    0.02 | 0.0916 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | 3           | ?                | 105.47 μs |  3.234 μs |  1.924 μs | 10.08 |    0.29 | 1.2207 | 1.0986 |  37.87 KB |       19.16 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | 3           | ?                |  51.04 μs |  0.156 μs |  0.104 μs |  4.88 |    0.11 | 0.4883 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |                  |           |           |           |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | MediumRun  | 15             | 2           | 10          | ?                |  10.86 μs |  0.273 μs |  0.391 μs |  1.00 |    0.05 | 0.0763 | 0.0610 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | MediumRun  | 15             | 2           | 10          | ?                |  10.76 μs |  0.045 μs |  0.067 μs |  0.99 |    0.03 | 0.0916 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | MediumRun  | 15             | 2           | 10          | ?                | 104.71 μs |  1.435 μs |  1.915 μs |  9.65 |    0.37 | 0.7324 | 0.6104 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | MediumRun  | 15             | 2           | 10          | ?                |  51.13 μs |  0.194 μs |  0.290 μs |  4.71 |    0.16 | 0.4883 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |                  |           |           |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **79.12 μs** |  **2.310 μs** |  **1.208 μs** |     **?** |       **?** | **0.6104** | **0.2441** |  **17.62 KB** |           **?** |
|                                     |            |                |             |             |                  |           |           |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 10               |  80.01 μs |  0.588 μs |  0.825 μs |     ? |       ? | 0.6104 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |             |                  |           |           |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **413.71 μs** | **12.035 μs** |  **6.294 μs** |     **?** |       **?** | **3.4180** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |             |                  |           |           |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 50               | 418.71 μs |  6.702 μs |  9.612 μs |     ? |       ? | 3.4180 | 1.4648 |  87.48 KB |           ? |
|                                     |            |                |             |             |                  |           |           |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **818.90 μs** | **13.358 μs** |  **6.986 μs** |     **?** |       **?** | **6.8359** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |             |                  |           |           |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 100              | 837.88 μs | 13.104 μs | 18.794 μs |     ? |       ? | 6.8359 | 2.9297 | 174.78 KB |           ? |
