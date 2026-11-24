```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev      | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|------------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |  **11.485 μs** |     **0.6341 μs** |   **0.3773 μs** |  **1.00** |    **0.04** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   8.682 μs |     0.0575 μs |   0.0380 μs |  0.76 |    0.02 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                | 109.116 μs |     7.0864 μs |   4.2170 μs |  9.51 |    0.46 |  2.1973 | 1.4648 |  43.31 KB |       17.27 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  42.908 μs |     0.2883 μs |   0.1907 μs |  3.74 |    0.12 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |             |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |  10.529 μs |     6.6120 μs |   0.3624 μs |  1.00 |    0.04 |  0.1984 | 0.1068 |   3.58 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   8.572 μs |     0.7247 μs |   0.0397 μs |  0.81 |    0.02 |  0.1984 |      - |    3.4 KB |        0.95 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 124.255 μs |   528.7759 μs |  28.9840 μs | 11.81 |    2.41 |  1.4648 | 0.7324 |   24.3 KB |        6.79 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  41.962 μs |     2.2882 μs |   0.1254 μs |  3.99 |    0.12 |  0.9766 |      - |  16.41 KB |        4.58 |
|                                     |            |                |             |                  |            |               |             |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **78.730 μs** |     **2.8630 μs** |   **1.4974 μs** |     **?** |       **?** |  **1.3428** | **0.3662** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  82.457 μs |   239.0850 μs |  13.1051 μs |     ? |       ? |  1.3428 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **381.764 μs** |    **14.2330 μs** |   **7.4441 μs** |     **?** |       **?** |  **6.3477** | **1.9531** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 399.151 μs |   958.8236 μs |  52.5564 μs |     ? |       ? |  6.3477 | 1.9531 |  111.3 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **736.205 μs** |    **37.9905 μs** |  **19.8698 μs** |     **?** |       **?** | **12.6953** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 799.014 μs | 1,902.2209 μs | 104.2671 μs |     ? |       ? | 12.6953 | 3.9063 | 222.45 KB |           ? |
