```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.866 μs** |   **0.6364 μs** |  **0.3787 μs** |  **1.00** |    **0.05** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   8.630 μs |   0.0397 μs |  0.0236 μs |  0.88 |    0.03 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  99.525 μs |   5.3001 μs |  3.1540 μs | 10.10 |    0.48 |  2.1973 | 1.4648 |  43.25 KB |       17.25 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  42.378 μs |   0.6019 μs |  0.3582 μs |  4.30 |    0.16 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.453 μs |   3.1014 μs |  0.1700 μs |  1.00 |    0.02 |  0.1984 | 0.1068 |   3.61 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   8.359 μs |   0.7602 μs |  0.0417 μs |  0.88 |    0.01 |  0.1984 |      - |    3.4 KB |        0.94 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 111.680 μs | 503.2101 μs | 27.5826 μs | 11.82 |    2.53 |  1.4648 | 0.7324 |   24.3 KB |        6.74 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  41.793 μs |   1.2984 μs |  0.0712 μs |  4.42 |    0.07 |  0.9766 |      - |  16.41 KB |        4.55 |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **71.527 μs** |   **2.3270 μs** |  **1.2171 μs** |     **?** |       **?** |  **1.3428** | **0.4883** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  76.952 μs | 177.5284 μs |  9.7309 μs |     ? |       ? |  1.3428 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **354.437 μs** |   **9.6928 μs** |  **5.0695 μs** |     **?** |       **?** |  **6.3477** | **1.9531** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 371.178 μs | 966.7952 μs | 52.9933 μs |     ? |       ? |  6.3477 | 1.9531 |  111.3 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **706.489 μs** |  **31.0685 μs** | **16.2494 μs** |     **?** |       **?** | **12.6953** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 697.149 μs | 328.0594 μs | 17.9820 μs |     ? |       ? | 12.6953 | 3.9063 | 222.45 KB |           ? |
