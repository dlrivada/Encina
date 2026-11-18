```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **9.935 μs** |   **0.7141 μs** |  **0.4250 μs** |  **1.00** |    **0.06** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   8.321 μs |   0.0337 μs |  0.0176 μs |  0.84 |    0.04 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                | 100.144 μs |   6.2534 μs |  3.7213 μs | 10.10 |    0.55 |  2.1973 | 1.4648 |  43.05 KB |       17.17 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  41.850 μs |   0.1708 μs |  0.1130 μs |  4.22 |    0.18 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   9.471 μs |   4.7370 μs |  0.2597 μs |  1.00 |    0.03 |  0.1526 | 0.0610 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   8.412 μs |   0.3080 μs |  0.0169 μs |  0.89 |    0.02 |  0.1984 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                | 110.192 μs | 511.2475 μs | 28.0232 μs | 11.64 |    2.58 |  1.4648 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  42.122 μs |   2.8293 μs |  0.1551 μs |  4.45 |    0.11 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **74.767 μs** |   **9.7484 μs** |  **5.8011 μs** |     **?** |       **?** |  **1.3428** | **0.3662** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  78.859 μs | 211.2294 μs | 11.5782 μs |     ? |       ? |  1.3428 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **362.517 μs** |  **18.4157 μs** |  **9.6318 μs** |     **?** |       **?** |  **6.3477** | **1.9531** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 377.849 μs | 942.5042 μs | 51.6618 μs |     ? |       ? |  6.3477 | 1.9531 |  111.3 KB |           ? |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **717.700 μs** |  **46.6612 μs** | **24.4047 μs** |     **?** |       **?** | **12.6953** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |            |             |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 729.610 μs | 148.4449 μs |  8.1368 μs |     ? |       ? | 12.6953 | 3.9063 | 222.45 KB |           ? |
