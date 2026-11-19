```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.236 μs** |     **0.2858 μs** |   **0.1495 μs** |  **1.00** |    **0.02** | **0.0305** | **0.0153** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   8.323 μs |     0.0401 μs |   0.0238 μs |  0.90 |    0.01 | 0.0305 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  89.571 μs |     3.6828 μs |   2.1916 μs |  9.70 |    0.27 | 0.3662 | 0.2441 |  43.14 KB |       17.20 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  41.837 μs |     0.1016 μs |   0.0672 μs |  4.53 |    0.07 | 0.1831 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.135 μs |     3.9715 μs |   0.2177 μs |  1.00 |    0.03 | 0.0305 | 0.0153 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   8.720 μs |     0.5905 μs |   0.0324 μs |  0.95 |    0.02 | 0.0305 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 105.251 μs |   485.1638 μs |  26.5935 μs | 11.53 |    2.53 | 0.2441 | 0.1221 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  41.854 μs |     1.5845 μs |   0.0869 μs |  4.58 |    0.10 | 0.1831 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **67.111 μs** |     **1.9187 μs** |   **1.0035 μs** |     **?** |       **?** | **0.2441** | **0.1221** |  **22.38 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  71.823 μs |   139.9974 μs |   7.6737 μs |     ? |       ? | 0.2441 | 0.1221 |  22.38 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **326.700 μs** |    **10.7135 μs** |   **5.6034 μs** |     **?** |       **?** | **0.9766** | **0.4883** | **111.29 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 354.106 μs |   945.3783 μs |  51.8194 μs |     ? |       ? | 0.9766 | 0.4883 | 111.29 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **668.425 μs** |    **27.2178 μs** |  **14.2354 μs** |     **?** |       **?** | **1.9531** | **0.9766** | **222.43 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 716.195 μs | 2,035.1674 μs | 111.5544 μs |     ? |       ? | 1.9531 | 0.9766 | 222.43 KB |           ? |
