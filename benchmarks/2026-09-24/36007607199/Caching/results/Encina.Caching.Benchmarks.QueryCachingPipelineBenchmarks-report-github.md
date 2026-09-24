```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **7.337 μs** |     **0.5754 μs** |  **0.3424 μs** |  **1.00** |    **0.06** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   6.654 μs |     0.0505 μs |  0.0334 μs |  0.91 |    0.04 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  72.949 μs |     5.0976 μs |  3.0335 μs |  9.96 |    0.59 | 1.9531 | 1.3428 |  37.72 KB |       19.08 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  31.583 μs |     0.3471 μs |  0.2296 μs |  4.31 |    0.19 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   6.988 μs |     1.9592 μs |  0.1074 μs |  1.00 |    0.02 | 0.1678 | 0.1068 |   3.08 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   6.544 μs |     0.2685 μs |  0.0147 μs |  0.94 |    0.01 | 0.1526 |      - |   2.55 KB |        0.83 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  85.537 μs |   452.0777 μs | 24.7799 μs | 12.24 |    3.08 | 1.0986 | 0.4883 |  18.99 KB |        6.16 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  32.068 μs |     0.9955 μs |  0.0546 μs |  4.59 |    0.06 | 0.7324 |      - |  12.19 KB |        3.96 |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **56.381 μs** |     **3.3433 μs** |  **1.9896 μs** |     **?** |       **?** | **1.4648** | **0.7324** |  **27.14 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  61.061 μs |   217.6810 μs | 11.9318 μs |     ? |       ? | 1.0376 | 0.3052 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **263.424 μs** |     **5.1772 μs** |  **2.7078 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 291.417 μs |   914.4292 μs | 50.1230 μs |     ? |       ? | 4.8828 | 1.4648 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **556.767 μs** |    **51.9008 μs** | **27.1451 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 617.938 μs | 1,772.8435 μs | 97.1755 μs |     ? |       ? | 9.7656 | 2.9297 | 174.78 KB |           ? |
