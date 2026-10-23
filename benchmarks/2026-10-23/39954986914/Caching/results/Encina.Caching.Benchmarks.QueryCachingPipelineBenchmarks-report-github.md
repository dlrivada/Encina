```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean      | Error        | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |----------:|-------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |  **12.20 μs** |     **0.164 μs** |  **0.098 μs** |  **1.00** |    **0.01** | **0.0305** | **0.0153** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |  11.57 μs |     0.034 μs |  0.020 μs |  0.95 |    0.01 | 0.0305 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                | 119.00 μs |     2.764 μs |  1.645 μs |  9.75 |    0.15 | 0.3662 | 0.2441 |  43.29 KB |       17.26 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  56.30 μs |     0.290 μs |  0.152 μs |  4.61 |    0.04 | 0.1831 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |           |              |           |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |  12.04 μs |     2.192 μs |  0.120 μs |  1.00 |    0.01 | 0.0305 | 0.0153 |    3.6 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |  11.59 μs |     1.949 μs |  0.107 μs |  0.96 |    0.01 | 0.0305 |      - |    3.4 KB |        0.94 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 136.40 μs |   606.988 μs | 33.271 μs | 11.33 |    2.39 | 0.2441 | 0.1221 |   24.3 KB |        6.75 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  57.18 μs |    16.896 μs |  0.926 μs |  4.75 |    0.08 | 0.1831 |      - |  16.41 KB |        4.56 |
|                                     |            |                |             |                  |           |              |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **88.62 μs** |     **2.659 μs** |  **1.391 μs** |     **?** |       **?** | **0.2441** | **0.1221** |  **22.38 KB** |           **?** |
|                                     |            |                |             |                  |           |              |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  95.28 μs |   179.363 μs |  9.831 μs |     ? |       ? | 0.2441 | 0.1221 |  22.38 KB |           ? |
|                                     |            |                |             |                  |           |              |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **461.06 μs** |    **14.704 μs** |  **7.691 μs** |     **?** |       **?** | **0.9766** | **0.4883** | **111.29 KB** |           **?** |
|                                     |            |                |             |                  |           |              |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 484.88 μs | 1,302.593 μs | 71.400 μs |     ? |       ? | 0.9766 | 0.4883 | 111.29 KB |           ? |
|                                     |            |                |             |                  |           |              |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **872.30 μs** |    **28.752 μs** | **15.038 μs** |     **?** |       **?** | **1.9531** | **0.9766** | **222.43 KB** |           **?** |
|                                     |            |                |             |                  |           |              |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 883.48 μs |   351.923 μs | 19.290 μs |     ? |       ? | 1.9531 | 0.9766 | 222.43 KB |           ? |
