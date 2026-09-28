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
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.074 μs** |     **0.6924 μs** |   **0.4120 μs** |  **1.00** |    **0.06** | **0.0229** | **0.0153** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   7.887 μs |     0.0214 μs |   0.0127 μs |  0.87 |    0.04 | 0.0305 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  73.860 μs |     4.6213 μs |   2.7501 μs |  8.15 |    0.46 | 0.3662 | 0.2441 |  37.96 KB |       19.21 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  37.393 μs |     0.0596 μs |   0.0312 μs |  4.13 |    0.18 | 0.1221 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   7.536 μs |     3.5231 μs |   0.1931 μs |  1.00 |    0.03 | 0.0153 |      - |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   7.691 μs |     0.5605 μs |   0.0307 μs |  1.02 |    0.02 | 0.0305 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  92.180 μs |   601.4035 μs |  32.9650 μs | 12.24 |    3.80 | 0.1221 |      - |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  40.057 μs |     2.6652 μs |   0.1461 μs |  5.32 |    0.12 | 0.1221 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **66.718 μs** |     **1.5100 μs** |   **0.8986 μs** |     **?** |       **?** | **0.2441** | **0.1831** |  **27.02 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  75.150 μs |   308.6241 μs |  16.9167 μs |     ? |       ? | 0.1831 | 0.1221 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **319.850 μs** |     **4.6815 μs** |   **2.4485 μs** |     **?** |       **?** | **0.9766** | **0.4883** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 343.126 μs |   903.6913 μs |  49.5344 μs |     ? |       ? | 0.9766 | 0.4883 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **634.796 μs** |    **13.5501 μs** |   **7.0870 μs** |     **?** |       **?** | **1.9531** | **0.9766** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |             |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 689.908 μs | 1,913.5750 μs | 104.8895 μs |     ? |       ? | 1.9531 | 0.9766 | 174.78 KB |           ? |
