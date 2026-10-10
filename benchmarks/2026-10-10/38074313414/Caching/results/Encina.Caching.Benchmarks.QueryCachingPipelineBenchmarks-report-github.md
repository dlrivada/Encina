```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **6.190 μs** |     **0.7940 μs** |  **0.4725 μs** |  **1.01** |    **0.10** |  **0.1144** | **0.0534** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   4.212 μs |     0.1512 μs |  0.1000 μs |  0.68 |    0.05 |  0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  64.202 μs |     6.1351 μs |  3.6509 μs | 10.42 |    0.93 |  1.1597 | 0.5493 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  20.958 μs |     0.6409 μs |  0.4239 μs |  3.40 |    0.25 |  0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   6.135 μs |     1.7279 μs |  0.0947 μs |  1.00 |    0.02 |  0.1144 | 0.0534 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   4.337 μs |     1.3147 μs |  0.0721 μs |  0.71 |    0.01 |  0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  76.921 μs |   476.8030 μs | 26.1352 μs | 12.54 |    3.69 |  1.1597 | 0.5493 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  21.299 μs |     5.1770 μs |  0.2838 μs |  3.47 |    0.06 |  0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **42.769 μs** |     **3.1441 μs** |  **1.6444 μs** |     **?** |       **?** |  **1.4648** | **0.6714** |  **27.03 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  48.131 μs |   197.2163 μs | 10.8101 μs |     ? |       ? |  1.0376 | 0.3662 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **229.520 μs** |    **42.4798 μs** | **25.2791 μs** |     **?** |       **?** |  **5.1270** | **1.7090** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 238.841 μs |   579.5130 μs | 31.7651 μs |     ? |       ? |  5.1270 | 1.7090 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **468.114 μs** |    **70.8677 μs** | **42.1722 μs** |     **?** |       **?** | **10.2539** | **3.4180** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 507.592 μs | 1,469.5764 μs | 80.5524 μs |     ? |       ? | 10.2539 | 3.4180 | 174.78 KB |           ? |
