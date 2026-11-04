```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.876 μs** |     **0.3387 μs** |   **0.2016 μs** |  **1.00** |    **0.03** | **0.0305** | **0.0153** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   8.368 μs |     0.0355 μs |   0.0211 μs |  0.85 |    0.02 | 0.0305 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  96.089 μs |     3.3086 μs |   1.9689 μs |  9.73 |    0.27 | 0.3662 | 0.2441 |  43.29 KB |       17.26 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  42.090 μs |     0.1335 μs |   0.0794 μs |  4.26 |    0.08 | 0.1831 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.566 μs |     2.2480 μs |   0.1232 μs |  1.00 |    0.02 | 0.0305 | 0.0153 |   3.58 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   8.466 μs |     0.3001 μs |   0.0164 μs |  0.89 |    0.01 | 0.0305 |      - |    3.4 KB |        0.95 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 112.653 μs |   659.1382 μs |  36.1296 μs | 11.78 |    3.27 | 0.2441 | 0.1221 |   24.3 KB |        6.79 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  41.474 μs |     0.3904 μs |   0.0214 μs |  4.34 |    0.05 | 0.1831 |      - |  16.41 KB |        4.58 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **70.773 μs** |     **2.6027 μs** |   **1.3613 μs** |     **?** |       **?** | **0.2441** | **0.1221** |  **22.38 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  76.190 μs |   148.9592 μs |   8.1650 μs |     ? |       ? | 0.2441 | 0.1221 |  22.38 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **343.390 μs** |    **17.3253 μs** |   **9.0614 μs** |     **?** |       **?** | **0.9766** | **0.4883** | **111.29 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 385.105 μs | 1,195.3546 μs |  65.5214 μs |     ? |       ? | 0.9766 | 0.4883 | 111.29 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **669.859 μs** |    **16.7531 μs** |   **8.7622 μs** |     **?** |       **?** | **1.9531** | **0.9766** | **222.43 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 741.971 μs | 2,097.1995 μs | 114.9546 μs |     ? |       ? | 1.9531 | 0.9766 | 222.43 KB |           ? |
