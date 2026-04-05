```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                              | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | concurrencyLevel | Mean          | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |----------------- |--------------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **?**                |      **9.738 μs** |  **0.3508 μs** | **0.2087 μs** |  **1.00** |    **0.03** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |      9.513 μs |  0.0076 μs | 0.0045 μs |  0.98 |    0.02 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |     98.337 μs |  3.4912 μs | 1.8260 μs | 10.10 |    0.27 | 1.9531 | 1.3428 |  37.87 KB |       19.16 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |     48.816 μs |  0.1202 μs | 0.0715 μs |  5.02 |    0.10 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |              |             |                  |               |            |           |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 42,601.827 μs |         NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |   3.44 KB |        1.00 |
| Pipeline_CacheHit                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 44,330.375 μs |         NA | 0.0000 μs |  1.04 |    0.00 |      - |      - |   7.33 KB |        2.13 |
| Pipeline_SequentialDifferentQueries | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 42,254.516 μs |         NA | 0.0000 μs |  0.99 |    0.00 |      - |      - |  23.76 KB |        6.91 |
| Pipeline_SequentialSameQuery        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 42,293.820 μs |         NA | 0.0000 μs |  0.99 |    0.00 |      - |      - |  16.96 KB |        4.93 |
|                                     |            |                |             |             |              |             |                  |               |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **10**               |     **77.340 μs** |  **1.4988 μs** | **0.7839 μs** |     **?** |       **?** | **0.9766** | **0.3662** |  **17.62 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |               |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10               | 46,207.347 μs |         NA | 0.0000 μs |     ? |       ? |      - |      - |  22.39 KB |           ? |
|                                     |            |                |             |             |              |             |                  |               |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **50**               |    **370.003 μs** | **10.6205 μs** | **5.5547 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |               |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 50               | 47,427.099 μs |         NA | 0.0000 μs |     ? |       ? |      - |      - |  96.91 KB |           ? |
|                                     |            |                |             |             |              |             |                  |               |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **100**              |    **771.213 μs** | **10.9434 μs** | **5.7236 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |               |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 100              | 47,820.066 μs |         NA | 0.0000 μs |     ? |       ? |      - |      - | 196.19 KB |           ? |
