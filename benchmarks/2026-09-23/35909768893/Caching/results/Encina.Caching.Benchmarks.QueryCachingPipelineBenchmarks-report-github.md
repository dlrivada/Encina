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
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **6.068 μs** |     **0.6304 μs** |  **0.3752 μs** |  **1.00** |    **0.08** |  **0.1144** | **0.0534** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   4.124 μs |     0.0516 μs |  0.0307 μs |  0.68 |    0.04 |  0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  63.539 μs |     6.9790 μs |  4.1531 μs | 10.51 |    0.89 |  1.1597 | 0.5493 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  20.257 μs |     0.3248 μs |  0.2148 μs |  3.35 |    0.19 |  0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   6.151 μs |     1.8013 μs |  0.0987 μs |  1.00 |    0.02 |  0.1144 | 0.0534 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   4.354 μs |     0.3176 μs |  0.0174 μs |  0.71 |    0.01 |  0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  71.902 μs |   403.8039 μs | 22.1339 μs | 11.69 |    3.12 |  1.0986 | 0.4883 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  20.883 μs |     6.5748 μs |  0.3604 μs |  3.40 |    0.07 |  0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **45.919 μs** |     **2.6751 μs** |  **1.5919 μs** |     **?** |       **?** |  **1.4648** | **0.7324** |  **26.98 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  52.630 μs |   228.2328 μs | 12.5102 μs |     ? |       ? |  1.0376 | 0.3662 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **231.560 μs** |    **38.6385 μs** | **22.9932 μs** |     **?** |       **?** |  **5.1270** | **1.7090** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 243.571 μs |   707.3588 μs | 38.7727 μs |     ? |       ? |  5.1270 | 1.7090 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **449.050 μs** |    **46.9198 μs** | **24.5400 μs** |     **?** |       **?** | **10.2539** | **3.4180** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 480.212 μs | 1,730.7437 μs | 94.8679 μs |     ? |       ? | 10.2539 | 3.4180 | 174.78 KB |           ? |
