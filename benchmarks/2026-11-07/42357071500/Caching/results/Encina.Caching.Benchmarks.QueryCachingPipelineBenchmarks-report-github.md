```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |------------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |    **14.01 μs** |   **0.546 μs** |  **0.325 μs** |  **1.00** |    **0.03** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |    12.62 μs |   0.022 μs |  0.015 μs |  0.90 |    0.02 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |   129.04 μs |   5.062 μs |  2.647 μs |  9.21 |    0.27 |  1.4648 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |    65.51 μs |   0.255 μs |  0.134 μs |  4.68 |    0.10 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |    13.21 μs |   4.398 μs |  0.241 μs |  1.00 |    0.02 |  0.1526 | 0.0610 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |    12.54 μs |   0.083 μs |  0.005 μs |  0.95 |    0.01 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |   146.21 μs | 420.362 μs | 23.041 μs | 11.07 |    1.52 |  1.4648 | 0.4883 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |    64.01 μs |   4.520 μs |  0.248 μs |  4.85 |    0.08 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |   **103.35 μs** |   **1.944 μs** |  **1.017 μs** |     **?** |       **?** |  **1.3428** | **0.4883** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |   109.68 μs | 222.071 μs | 12.172 μs |     ? |       ? |  1.3428 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               |   **484.86 μs** |  **12.037 μs** |  **7.163 μs** |     **?** |       **?** |  **5.8594** | **1.9531** | **111.31 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               |   491.63 μs |  10.479 μs |  0.574 μs |     ? |       ? |  5.8594 | 1.9531 | 111.31 KB |           ? |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **1,002.48 μs** |  **28.785 μs** | **17.130 μs** |     **?** |       **?** | **11.7188** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              |   980.37 μs |  62.309 μs |  3.415 μs |     ? |       ? | 11.7188 | 3.9063 | 222.45 KB |           ? |
