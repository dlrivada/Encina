```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                              | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | concurrencyLevel | Mean          | Error      | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |----------------- |--------------:|-----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **?**                |      **9.620 μs** |  **0.3902 μs** |  **0.2322 μs** |  **1.00** |    **0.03** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |      9.938 μs |  0.0838 μs |  0.0499 μs |  1.03 |    0.02 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |     99.922 μs |  2.2733 μs |  1.1890 μs | 10.39 |    0.27 | 1.9531 | 1.4648 |   37.9 KB |       19.17 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |     47.503 μs |  0.2885 μs |  0.1908 μs |  4.94 |    0.12 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |              |             |                  |               |            |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 42,119.043 μs |         NA |  0.0000 μs |  1.00 |    0.00 |      - |      - |   3.44 KB |        1.00 |
| Pipeline_CacheHit                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 44,283.956 μs |         NA |  0.0000 μs |  1.05 |    0.00 |      - |      - |   7.33 KB |        2.13 |
| Pipeline_SequentialDifferentQueries | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 42,052.378 μs |         NA |  0.0000 μs |  1.00 |    0.00 |      - |      - |  23.76 KB |        6.91 |
| Pipeline_SequentialSameQuery        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 41,919.770 μs |         NA |  0.0000 μs |  1.00 |    0.00 |      - |      - |  16.96 KB |        4.93 |
|                                     |            |                |             |             |              |             |                  |               |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **10**               |     **75.223 μs** |  **1.4825 μs** |  **0.7754 μs** |     **?** |       **?** | **0.9766** | **0.2441** |  **17.62 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |               |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10               | 47,289.706 μs |         NA |  0.0000 μs |     ? |       ? |      - |      - |  22.39 KB |           ? |
|                                     |            |                |             |             |              |             |                  |               |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **50**               |    **375.535 μs** |  **9.1006 μs** |  **4.7598 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |               |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 50               | 47,122.507 μs |         NA |  0.0000 μs |     ? |       ? |      - |      - |  96.74 KB |           ? |
|                                     |            |                |             |             |              |             |                  |               |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **100**              |    **753.295 μs** | **30.3168 μs** | **15.8563 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |               |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 100              | 47,494.914 μs |         NA |  0.0000 μs |     ? |       ? |      - |      - | 189.12 KB |           ? |
