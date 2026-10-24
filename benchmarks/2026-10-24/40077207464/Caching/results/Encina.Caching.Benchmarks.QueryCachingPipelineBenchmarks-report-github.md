```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.946 μs** |     **0.3854 μs** |   **0.2294 μs** |  **1.00** |    **0.03** | **0.0305** | **0.0153** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   8.993 μs |     0.7863 μs |   0.5201 μs |  0.90 |    0.05 | 0.0305 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  95.866 μs |     4.9850 μs |   2.9665 μs |  9.64 |    0.35 | 0.3662 | 0.2441 |  43.14 KB |       17.20 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  41.674 μs |     1.3698 μs |   0.8152 μs |  4.19 |    0.12 | 0.1831 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.268 μs |     1.7479 μs |   0.0958 μs |  1.00 |    0.01 | 0.0305 | 0.0153 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   8.705 μs |     6.4061 μs |   0.3511 μs |  0.94 |    0.03 | 0.0305 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 106.879 μs |   613.4583 μs |  33.6257 μs | 11.53 |    3.14 | 0.2441 | 0.1221 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  40.940 μs |     2.5551 μs |   0.1401 μs |  4.42 |    0.04 | 0.1831 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **70.863 μs** |    **13.1006 μs** |   **7.7960 μs** |     **?** |       **?** | **0.2441** | **0.1221** |  **22.38 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  72.524 μs |   148.3121 μs |   8.1295 μs |     ? |       ? | 0.2441 | 0.1221 |  22.38 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **336.467 μs** |    **32.7035 μs** |  **17.1045 μs** |     **?** |       **?** | **0.9766** | **0.4883** | **111.29 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 370.725 μs |   944.8163 μs |  51.7886 μs |     ? |       ? | 0.9766 | 0.4883 | 111.29 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **659.114 μs** |    **49.1180 μs** |  **25.6897 μs** |     **?** |       **?** | **1.9531** | **0.9766** | **222.43 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 736.783 μs | 2,605.9271 μs | 142.8397 μs |     ? |       ? | 1.9531 | 0.9766 | 222.43 KB |           ? |
