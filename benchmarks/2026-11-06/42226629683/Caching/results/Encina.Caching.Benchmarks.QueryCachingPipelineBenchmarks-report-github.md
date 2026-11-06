```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |  **12.77 μs** |   **0.154 μs** |  **0.081 μs** |  **1.00** |    **0.01** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |  12.49 μs |   0.060 μs |  0.036 μs |  0.98 |    0.01 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                | 130.70 μs |   3.705 μs |  1.938 μs | 10.24 |    0.16 |  1.4648 | 0.4883 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  63.60 μs |   0.209 μs |  0.109 μs |  4.98 |    0.03 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |  12.56 μs |   3.130 μs |  0.172 μs |  1.00 |    0.02 |  0.1984 | 0.1068 |    3.6 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |  12.87 μs |   0.464 μs |  0.025 μs |  1.02 |    0.01 |  0.1984 |      - |    3.4 KB |        0.94 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 137.63 μs | 371.067 μs | 20.339 μs | 10.96 |    1.41 |  1.4648 | 0.4883 |   24.3 KB |        6.74 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  61.85 μs |   6.497 μs |  0.356 μs |  4.93 |    0.06 |  0.9766 |      - |  16.41 KB |        4.55 |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **100.32 μs** |   **1.507 μs** |  **0.788 μs** |     **?** |       **?** |  **1.3428** | **0.4883** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               | 104.91 μs | 213.923 μs | 11.726 μs |     ? |       ? |  1.3428 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **528.95 μs** |  **20.620 μs** | **12.270 μs** |     **?** |       **?** |  **5.8594** | **1.9531** | **111.31 KB** |           **?** |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 492.08 μs | 110.417 μs |  6.052 μs |     ? |       ? |  5.8594 | 1.9531 | 111.31 KB |           ? |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **992.08 μs** |  **19.147 μs** | **11.394 μs** |     **?** |       **?** | **11.7188** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |           |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 958.93 μs |  25.478 μs |  1.397 μs |     ? |       ? | 11.7188 | 3.9063 | 222.45 KB |           ? |
