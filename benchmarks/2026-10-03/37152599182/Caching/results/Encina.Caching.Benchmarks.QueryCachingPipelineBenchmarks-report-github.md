```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.72GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.942 μs** |     **0.4295 μs** |   **0.2556 μs** |  **1.00** |    **0.03** | **0.0763** | **0.0610** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |  10.134 μs |     0.0289 μs |   0.0191 μs |  1.02 |    0.02 | 0.0916 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                | 100.814 μs |     2.0894 μs |   1.2434 μs | 10.15 |    0.27 | 1.2207 | 1.0986 |  37.81 KB |       19.13 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  48.879 μs |     0.1889 μs |   0.1250 μs |  4.92 |    0.12 | 0.4883 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.796 μs |     2.6878 μs |   0.1473 μs |  1.00 |    0.02 | 0.1068 | 0.0916 |   3.06 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |  10.421 μs |     0.2171 μs |   0.0119 μs |  1.06 |    0.01 | 0.0916 |      - |   2.55 KB |        0.83 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 114.009 μs |   446.1328 μs |  24.4540 μs | 11.64 |    2.17 | 0.7324 | 0.6104 |  18.99 KB |        6.20 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  50.596 μs |     1.0242 μs |   0.0561 μs |  5.17 |    0.07 | 0.4883 |      - |  12.19 KB |        3.98 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **75.778 μs** |     **2.7078 μs** |   **1.4162 μs** |     **?** |       **?** | **0.6104** | **0.2441** |  **17.62 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  83.673 μs |   256.4815 μs |  14.0586 μs |     ? |       ? | 0.6104 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **398.551 μs** |    **16.9986 μs** |   **8.8906 μs** |     **?** |       **?** | **3.4180** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 421.656 μs |   952.9219 μs |  52.2329 μs |     ? |       ? | 3.4180 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **795.037 μs** |    **39.2730 μs** |  **20.5405 μs** |     **?** |       **?** | **6.8359** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 853.839 μs | 2,148.4704 μs | 117.7649 μs |     ? |       ? | 6.8359 | 2.9297 | 174.78 KB |           ? |
