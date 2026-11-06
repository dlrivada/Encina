```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean      | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |----------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |  **12.47 μs** |     **0.419 μs** |   **0.249 μs** |  **1.00** |    **0.03** | **0.0305** | **0.0153** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |  11.22 μs |     0.052 μs |   0.034 μs |  0.90 |    0.02 | 0.0305 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                | 120.67 μs |     3.210 μs |   1.910 μs |  9.68 |    0.23 | 0.3662 | 0.2441 |   43.3 KB |       17.27 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  55.76 μs |     0.460 μs |   0.240 μs |  4.47 |    0.09 | 0.1831 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |  12.00 μs |     2.781 μs |   0.152 μs |  1.00 |    0.02 | 0.0305 | 0.0153 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |  11.11 μs |     0.326 μs |   0.018 μs |  0.93 |    0.01 | 0.0305 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 137.38 μs |   658.915 μs |  36.117 μs | 11.45 |    2.61 | 0.2441 | 0.1221 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  55.94 μs |     1.807 μs |   0.099 μs |  4.66 |    0.05 | 0.1831 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **92.28 μs** |     **1.839 μs** |   **0.962 μs** |     **?** |       **?** | **0.2441** | **0.1221** |  **22.38 KB** |           **?** |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  95.46 μs |   183.993 μs |  10.085 μs |     ? |       ? | 0.2441 | 0.1221 |  22.38 KB |           ? |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **453.15 μs** |    **13.484 μs** |   **7.052 μs** |     **?** |       **?** | **0.9766** | **0.4883** | **111.29 KB** |           **?** |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 479.53 μs | 1,359.410 μs |  74.514 μs |     ? |       ? | 0.9766 | 0.4883 | 111.29 KB |           ? |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **878.29 μs** |    **33.022 μs** |  **17.271 μs** |     **?** |       **?** | **1.9531** | **0.9766** | **222.43 KB** |           **?** |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 973.89 μs | 2,916.426 μs | 159.859 μs |     ? |       ? | 1.9531 | 0.9766 | 222.43 KB |           ? |
