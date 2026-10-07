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
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **6.130 μs** |     **1.1898 μs** |  **0.7080 μs** |  **1.01** |    **0.15** |  **0.1144** | **0.0534** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   4.307 μs |     0.0620 μs |  0.0410 μs |  0.71 |    0.07 |  0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  65.737 μs |     9.1614 μs |  5.4518 μs | 10.84 |    1.40 |  1.1597 | 0.5493 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  21.636 μs |     0.4080 μs |  0.2428 μs |  3.57 |    0.37 |  0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   5.775 μs |     2.5102 μs |  0.1376 μs |  1.00 |    0.03 |  0.1144 | 0.0534 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   4.710 μs |     0.6560 μs |  0.0360 μs |  0.82 |    0.02 |  0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  68.298 μs |   391.9184 μs | 21.4824 μs | 11.83 |    3.23 |  1.0986 | 0.4883 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  22.022 μs |    15.2741 μs |  0.8372 μs |  3.81 |    0.15 |  0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **44.711 μs** |     **4.6454 μs** |  **2.7644 μs** |     **?** |       **?** |  **1.4648** | **0.7935** |  **27.14 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  50.220 μs |   199.2019 μs | 10.9189 μs |     ? |       ? |  1.0376 | 0.3052 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **223.762 μs** |    **25.1316 μs** | **13.1443 μs** |     **?** |       **?** |  **5.1270** | **1.7090** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 241.327 μs |   757.1565 μs | 41.5023 μs |     ? |       ? |  5.1270 | 1.7090 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **464.927 μs** |    **77.1809 μs** | **45.9291 μs** |     **?** |       **?** | **10.2539** | **3.4180** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 493.017 μs | 1,352.8184 μs | 74.1525 μs |     ? |       ? | 10.2539 | 3.4180 | 174.78 KB |           ? |
