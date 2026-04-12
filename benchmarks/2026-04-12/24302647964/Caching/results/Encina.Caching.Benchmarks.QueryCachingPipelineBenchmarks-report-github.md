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
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |  **10.89 μs** |  **0.635 μs** |  **0.378 μs** |  **1.00** |    **0.05** | **0.0763** | **0.0610** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | 3           | ?                |  10.42 μs |  0.036 μs |  0.021 μs |  0.96 |    0.03 | 0.0916 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | 3           | ?                | 110.49 μs | 13.885 μs |  8.263 μs | 10.16 |    0.79 | 1.2207 | 1.0986 |  37.75 KB |       19.10 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | 3           | ?                |  50.11 μs |  0.207 μs |  0.137 μs |  4.61 |    0.15 | 0.4883 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |                  |           |           |           |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | MediumRun  | 15             | 2           | 10          | ?                |  11.23 μs |  0.449 μs |  0.643 μs |  1.00 |    0.08 | 0.0763 | 0.0610 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | MediumRun  | 15             | 2           | 10          | ?                |  10.78 μs |  0.112 μs |  0.161 μs |  0.96 |    0.05 | 0.0916 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | MediumRun  | 15             | 2           | 10          | ?                | 123.48 μs |  3.759 μs |  5.145 μs | 11.03 |    0.75 | 0.7324 | 0.4883 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | MediumRun  | 15             | 2           | 10          | ?                |  50.32 μs |  0.119 μs |  0.170 μs |  4.50 |    0.25 | 0.4883 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |                  |           |           |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **88.00 μs** |  **3.666 μs** |  **1.918 μs** |     **?** |       **?** | **0.6104** | **0.2441** |  **17.62 KB** |           **?** |
|                                     |            |                |             |             |                  |           |           |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 10               |  86.79 μs |  3.392 μs |  4.864 μs |     ? |       ? | 0.6104 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |             |                  |           |           |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **438.05 μs** | **59.521 μs** | **35.420 μs** |     **?** |       **?** | **3.4180** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |             |                  |           |           |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 50               | 445.56 μs | 17.954 μs | 25.750 μs |     ? |       ? | 3.4180 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |             |                  |           |           |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **826.69 μs** | **66.765 μs** | **34.919 μs** |     **?** |       **?** | **6.8359** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |             |                  |           |           |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 100              | 902.67 μs | 43.073 μs | 60.383 μs |     ? |       ? | 6.8359 | 2.9297 | 174.78 KB |           ? |
