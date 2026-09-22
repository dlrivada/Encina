```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **7.800 μs** |     **0.4421 μs** |   **0.2631 μs** |  **1.00** |    **0.05** | **0.0153** |      **-** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   8.428 μs |     0.1973 μs |   0.1305 μs |  1.08 |    0.04 | 0.0305 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  81.238 μs |     4.5139 μs |   2.6861 μs | 10.43 |    0.47 | 0.3662 | 0.2441 |  37.51 KB |       18.98 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  39.742 μs |     0.5902 μs |   0.3904 μs |  5.10 |    0.17 | 0.1221 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   7.985 μs |     2.8875 μs |   0.1583 μs |  1.00 |    0.02 | 0.0305 | 0.0153 |   3.05 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   8.059 μs |     1.1663 μs |   0.0639 μs |  1.01 |    0.02 | 0.0305 |      - |   2.55 KB |        0.84 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  99.785 μs |   658.3553 μs |  36.0867 μs | 12.50 |    3.92 | 0.1221 |      - |  18.99 KB |        6.22 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  40.683 μs |     1.8718 μs |   0.1026 μs |  5.10 |    0.09 | 0.1221 |      - |  12.19 KB |        3.99 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **63.402 μs** |     **5.5434 μs** |   **2.8993 μs** |     **?** |       **?** | **0.1221** |      **-** |  **17.62 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  70.229 μs |   262.8748 μs |  14.4091 μs |     ? |       ? | 0.1221 |      - |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **340.234 μs** |    **13.4802 μs** |   **7.0504 μs** |     **?** |       **?** | **0.9766** | **0.4883** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 368.160 μs | 1,056.1073 μs |  57.8888 μs |     ? |       ? | 0.9766 | 0.4883 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **666.931 μs** |    **25.4961 μs** |  **13.3349 μs** |     **?** |       **?** | **1.9531** | **0.9766** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 724.361 μs | 1,898.6966 μs | 104.0740 μs |     ? |       ? | 1.9531 | 0.9766 | 174.78 KB |           ? |
