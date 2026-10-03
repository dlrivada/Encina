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
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **8.293 μs** |     **0.4026 μs** |   **0.2396 μs** |  **1.00** |    **0.04** | **0.0229** | **0.0153** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   7.899 μs |     0.1125 μs |   0.0670 μs |  0.95 |    0.03 | 0.0305 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  72.745 μs |     4.9045 μs |   2.9186 μs |  8.78 |    0.41 | 0.3662 | 0.2441 |  37.92 KB |       19.19 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  40.493 μs |     0.4738 μs |   0.2478 μs |  4.89 |    0.14 | 0.1221 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   8.524 μs |     3.7229 μs |   0.2041 μs |  1.00 |    0.03 | 0.0229 | 0.0153 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   8.143 μs |     0.7297 μs |   0.0400 μs |  0.96 |    0.02 | 0.0305 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  93.808 μs |   563.9756 μs |  30.9134 μs | 11.01 |    3.15 | 0.1221 |      - |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  41.330 μs |     3.4136 μs |   0.1871 μs |  4.85 |    0.10 | 0.1221 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **65.020 μs** |     **2.9623 μs** |   **1.7628 μs** |     **?** |       **?** | **0.2441** | **0.1831** |  **27.01 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  71.053 μs |   227.7821 μs |  12.4855 μs |     ? |       ? | 0.1221 |      - |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **340.776 μs** |    **11.5654 μs** |   **6.0490 μs** |     **?** |       **?** | **0.9766** | **0.4883** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 339.995 μs |   806.1724 μs |  44.1890 μs |     ? |       ? | 0.9766 | 0.4883 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **659.508 μs** |    **18.5080 μs** |   **9.6801 μs** |     **?** |       **?** | **1.9531** | **0.9766** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 726.068 μs | 1,825.9659 μs | 100.0874 μs |     ? |       ? | 1.9531 | 0.9766 | 174.78 KB |           ? |
