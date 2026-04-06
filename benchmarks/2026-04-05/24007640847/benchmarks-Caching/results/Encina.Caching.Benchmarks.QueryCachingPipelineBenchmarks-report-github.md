```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                              | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | concurrencyLevel | Mean          | Error      | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |----------------- |--------------:|-----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **?**                |      **9.835 μs** |  **0.4365 μs** |  **0.2597 μs** |  **1.00** |    **0.04** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |      9.127 μs |  0.0234 μs |  0.0155 μs |  0.93 |    0.02 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |     99.034 μs |  3.4045 μs |  1.7806 μs | 10.08 |    0.30 | 1.9531 | 1.4648 |  37.94 KB |       19.19 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |     45.787 μs |  0.1212 μs |  0.0802 μs |  4.66 |    0.12 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |              |             |                  |               |            |            |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 42,530.792 μs |         NA |  0.0000 μs |  1.00 |    0.00 |      - |      - |   3.44 KB |        1.00 |
| Pipeline_CacheHit                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 43,349.932 μs |         NA |  0.0000 μs |  1.02 |    0.00 |      - |      - |   7.33 KB |        2.13 |
| Pipeline_SequentialDifferentQueries | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 44,613.562 μs |         NA |  0.0000 μs |  1.05 |    0.00 |      - |      - |  23.76 KB |        6.91 |
| Pipeline_SequentialSameQuery        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 43,523.576 μs |         NA |  0.0000 μs |  1.02 |    0.00 |      - |      - |  16.96 KB |        4.93 |
|                                     |            |                |             |             |              |             |                  |               |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **10**               |     **75.354 μs** |  **1.4184 μs** |  **0.7419 μs** |     **?** |       **?** | **0.9766** | **0.2441** |  **17.62 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |               |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10               | 46,741.149 μs |         NA |  0.0000 μs |     ? |       ? |      - |      - |  22.39 KB |           ? |
|                                     |            |                |             |             |              |             |                  |               |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **50**               |    **368.559 μs** |  **7.9995 μs** |  **4.1839 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |               |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 50               | 47,002.729 μs |         NA |  0.0000 μs |     ? |       ? |      - |      - |  92.23 KB |           ? |
|                                     |            |                |             |             |              |             |                  |               |            |            |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **100**              |    **756.566 μs** | **26.5344 μs** | **13.8780 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |               |            |            |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 100              | 47,270.241 μs |         NA |  0.0000 μs |     ? |       ? |      - |      - | 188.69 KB |           ? |
