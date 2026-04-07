```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean       | Error      | StdDev     | Median     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |----------------- |-----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |   **9.603 μs** |  **0.2234 μs** |  **0.1329 μs** |   **9.639 μs** |  **1.00** |    **0.02** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | 3           | ?                |   9.655 μs |  0.0827 μs |  0.0547 μs |   9.658 μs |  1.01 |    0.01 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | 3           | ?                |  95.591 μs |  3.0268 μs |  1.5831 μs |  96.030 μs |  9.96 |    0.20 | 1.9531 | 1.4648 |  38.12 KB |       19.28 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | 3           | ?                |  46.930 μs |  0.0651 μs |  0.0430 μs |  46.933 μs |  4.89 |    0.06 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | MediumRun  | 15             | 2           | 10          | ?                |  10.096 μs |  0.2490 μs |  0.3572 μs |  10.062 μs |  1.00 |    0.05 | 0.1068 | 0.0458 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | MediumRun  | 15             | 2           | 10          | ?                |   9.510 μs |  0.0908 μs |  0.1273 μs |   9.403 μs |  0.94 |    0.03 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | MediumRun  | 15             | 2           | 10          | ?                | 102.081 μs |  1.5969 μs |  2.1859 μs | 102.001 μs | 10.12 |    0.41 | 1.0986 | 0.4883 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | MediumRun  | 15             | 2           | 10          | ?                |  47.859 μs |  0.0699 μs |  0.1046 μs |  47.862 μs |  4.75 |    0.16 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **74.788 μs** |  **1.7715 μs** |  **0.9265 μs** |  **75.088 μs** |     **?** |       **?** | **0.9766** | **0.3662** |  **17.62 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 10               |  76.606 μs |  0.6804 μs |  0.9314 μs |  76.556 μs |     ? |       ? | 0.9766 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **366.903 μs** |  **8.1803 μs** |  **4.2784 μs** | **367.258 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 50               | 377.966 μs |  2.6775 μs |  3.5744 μs | 377.619 μs |     ? |       ? | 4.8828 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **739.293 μs** | **16.1772 μs** |  **8.4610 μs** | **736.934 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 100              | 770.315 μs | 17.1745 μs | 24.6312 μs | 771.833 μs |     ? |       ? | 9.7656 | 2.9297 | 174.79 KB |           ? |
