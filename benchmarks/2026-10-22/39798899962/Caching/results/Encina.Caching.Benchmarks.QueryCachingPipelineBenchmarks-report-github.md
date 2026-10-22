```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.687 μs** |   **0.6561 μs** |  **0.3904 μs** |  **1.00** |    **0.06** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   8.823 μs |   0.0591 μs |  0.0352 μs |  0.91 |    0.04 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  94.109 μs |   4.1773 μs |  2.1848 μs |  9.73 |    0.44 |  2.1973 | 1.4648 |  43.26 KB |       17.25 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  42.521 μs |   0.1643 μs |  0.0978 μs |  4.40 |    0.18 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.189 μs |   2.4945 μs |  0.1367 μs |  1.00 |    0.02 |  0.1984 | 0.1068 |   3.59 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   9.158 μs |   0.5613 μs |  0.0308 μs |  1.00 |    0.01 |  0.1984 |      - |    3.4 KB |        0.95 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 106.055 μs | 481.9193 μs | 26.4156 μs | 11.54 |    2.49 |  1.4648 | 0.7324 |   24.3 KB |        6.77 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  42.891 μs |   4.3873 μs |  0.2405 μs |  4.67 |    0.06 |  0.9766 |      - |  16.41 KB |        4.57 |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **68.399 μs** |   **1.9034 μs** |  **0.9955 μs** |     **?** |       **?** |  **1.3428** | **0.4883** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  73.346 μs | 192.8690 μs | 10.5718 μs |     ? |       ? |  1.3428 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **342.477 μs** |  **12.1739 μs** |  **6.3672 μs** |     **?** |       **?** |  **6.3477** | **1.9531** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 375.030 μs | 862.5956 μs | 47.2818 μs |     ? |       ? |  6.3477 | 1.9531 |  111.3 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **693.727 μs** |  **41.8739 μs** | **21.9008 μs** |     **?** |       **?** | **12.6953** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 689.609 μs | 289.2938 μs | 15.8572 μs |     ? |       ? | 12.6953 | 3.9063 | 222.45 KB |           ? |
