```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean      | Error        | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |----------:|-------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |  **10.28 μs** |     **0.276 μs** |  **0.144 μs** |  **1.00** |    **0.02** | **0.0763** | **0.0610** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |  10.28 μs |     0.027 μs |  0.016 μs |  1.00 |    0.01 | 0.0916 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                | 105.45 μs |     4.153 μs |  2.172 μs | 10.26 |    0.24 | 1.2207 | 1.0986 |  37.93 KB |       19.19 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  48.91 μs |     0.143 μs |  0.085 μs |  4.76 |    0.06 | 0.4883 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |           |              |           |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |  10.44 μs |     2.039 μs |  0.112 μs |  1.00 |    0.01 | 0.0763 | 0.0610 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |  10.46 μs |     0.548 μs |  0.030 μs |  1.00 |    0.01 | 0.0916 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 119.85 μs |   489.038 μs | 26.806 μs | 11.48 |    2.23 | 0.7324 | 0.6104 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  51.27 μs |     3.971 μs |  0.218 μs |  4.91 |    0.05 | 0.4883 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |           |              |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **77.49 μs** |     **1.884 μs** |  **0.985 μs** |     **?** |       **?** | **0.6104** | **0.2441** |  **17.62 KB** |           **?** |
|                                     |            |                |             |                  |           |              |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  86.28 μs |   271.650 μs | 14.890 μs |     ? |       ? | 0.6104 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |                  |           |              |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **402.85 μs** |     **9.581 μs** |  **5.011 μs** |     **?** |       **?** | **3.4180** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |           |              |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 441.42 μs | 1,181.809 μs | 64.779 μs |     ? |       ? | 3.4180 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |                  |           |              |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **803.57 μs** |    **17.771 μs** |  **9.294 μs** |     **?** |       **?** | **6.8359** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |           |              |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 790.70 μs |   155.440 μs |  8.520 μs |     ? |       ? | 6.8359 | 2.9297 | 174.78 KB |           ? |
