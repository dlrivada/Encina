```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.872 μs** |     **0.3669 μs** |  **0.2183 μs** |  **1.00** |    **0.03** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   9.177 μs |     0.0375 μs |  0.0248 μs |  0.93 |    0.02 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                | 102.803 μs |     3.4680 μs |  2.0638 μs | 10.42 |    0.30 | 1.9531 | 1.3428 |  37.96 KB |       19.21 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  48.503 μs |     0.1100 μs |  0.0728 μs |  4.92 |    0.10 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.512 μs |     2.1387 μs |  0.1172 μs |  1.00 |    0.02 | 0.1678 | 0.1068 |   3.08 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   9.419 μs |     0.4422 μs |  0.0242 μs |  0.99 |    0.01 | 0.1526 |      - |   2.55 KB |        0.83 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 110.325 μs |   356.6945 μs | 19.5516 μs | 11.60 |    1.78 | 1.0986 | 0.4883 |  18.99 KB |        6.17 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  47.724 μs |     1.1153 μs |  0.0611 μs |  5.02 |    0.05 | 0.7324 |      - |  12.19 KB |        3.96 |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **78.577 μs** |     **1.8244 μs** |  **0.9542 μs** |     **?** |       **?** | **0.9766** | **0.3662** |  **17.62 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  81.841 μs |   213.2893 μs | 11.6911 μs |     ? |       ? | 0.9766 | 0.2441 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **376.158 μs** |    **15.2401 μs** |  **9.0692 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 398.883 μs |   866.6053 μs | 47.5016 μs |     ? |       ? | 4.8828 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **749.274 μs** |    **23.7528 μs** | **12.4232 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 821.716 μs | 1,823.5174 μs | 99.9531 μs |     ? |       ? | 9.7656 | 2.9297 | 174.78 KB |           ? |
