```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev     | Median     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|-----------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **6.390 μs** |     **0.6899 μs** |  **0.4106 μs** |   **6.619 μs** |  **1.00** |    **0.09** |  **0.1144** | **0.0534** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   4.243 μs |     0.0679 μs |  0.0449 μs |   4.246 μs |  0.67 |    0.04 |  0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  57.009 μs |     3.8595 μs |  2.2967 μs |  57.733 μs |  8.95 |    0.65 |  1.9531 | 1.3428 |  38.01 KB |       19.23 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  21.496 μs |     0.4435 μs |  0.2639 μs |  21.597 μs |  3.38 |    0.21 |  0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   5.726 μs |     0.7280 μs |  0.0399 μs |   5.735 μs |  1.00 |    0.01 |  0.1144 | 0.0534 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   4.305 μs |     1.9219 μs |  0.1053 μs |   4.308 μs |  0.75 |    0.02 |  0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  70.835 μs |   475.3594 μs | 26.0561 μs |  56.076 μs | 12.37 |    3.94 |  1.1597 | 0.5493 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  20.702 μs |     7.7280 μs |  0.4236 μs |  20.899 μs |  3.62 |    0.07 |  0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **41.128 μs** |     **2.2757 μs** |  **1.1903 μs** |  **41.337 μs** |     **?** |       **?** |  **1.4648** | **0.7324** |  **26.94 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  47.510 μs |   189.1882 μs | 10.3700 μs |  43.801 μs |     ? |       ? |  1.0376 | 0.3662 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |            |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **218.184 μs** |    **39.5306 μs** | **23.5240 μs** | **204.532 μs** |     **?** |       **?** |  **5.1270** | **1.7090** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 233.979 μs |   809.5785 μs | 44.3757 μs | 214.070 μs |     ? |       ? |  5.1270 | 1.7090 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |            |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **446.171 μs** |    **74.4529 μs** | **44.3057 μs** | **444.774 μs** |     **?** |       **?** | **10.2539** | **3.4180** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 463.589 μs | 1,208.1687 μs | 66.2238 μs | 437.038 μs |     ? |       ? | 10.2539 | 3.4180 | 174.78 KB |           ? |
