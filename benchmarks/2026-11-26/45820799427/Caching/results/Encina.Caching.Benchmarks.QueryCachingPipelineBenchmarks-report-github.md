```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean      | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |----------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |  **11.70 μs** |     **0.355 μs** |   **0.211 μs** |  **1.00** |    **0.02** | **0.0305** | **0.0153** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |  11.07 μs |     0.147 μs |   0.097 μs |  0.95 |    0.02 | 0.0305 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                | 115.83 μs |     4.386 μs |   2.610 μs |  9.90 |    0.27 | 0.3662 | 0.2441 |  43.41 KB |       17.31 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  56.23 μs |     1.250 μs |   0.827 μs |  4.81 |    0.11 | 0.1831 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |  11.99 μs |     0.863 μs |   0.047 μs |  1.00 |    0.00 | 0.0305 | 0.0153 |   3.61 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |  11.30 μs |     0.277 μs |   0.015 μs |  0.94 |    0.00 | 0.0305 |      - |    3.4 KB |        0.94 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 135.48 μs |   651.030 μs |  35.685 μs | 11.30 |    2.58 | 0.2441 | 0.1221 |   24.3 KB |        6.74 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  54.58 μs |     0.660 μs |   0.036 μs |  4.55 |    0.02 | 0.1831 |      - |  16.41 KB |        4.55 |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **88.32 μs** |     **3.400 μs** |   **1.778 μs** |     **?** |       **?** | **0.2441** | **0.1221** |  **22.38 KB** |           **?** |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  94.02 μs |   181.085 μs |   9.926 μs |     ? |       ? | 0.2441 | 0.1221 |  22.38 KB |           ? |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **432.28 μs** |    **14.087 μs** |   **8.383 μs** |     **?** |       **?** | **0.9766** | **0.4883** | **111.29 KB** |           **?** |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 481.48 μs | 1,104.957 μs |  60.566 μs |     ? |       ? | 0.9766 | 0.4883 | 111.29 KB |           ? |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **852.87 μs** |    **27.363 μs** |  **14.311 μs** |     **?** |       **?** | **1.9531** | **0.9766** | **222.43 KB** |           **?** |
|                                     |            |                |             |                  |           |              |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 939.72 μs | 2,680.808 μs | 146.944 μs |     ? |       ? | 1.9531 | 0.9766 | 222.43 KB |           ? |
