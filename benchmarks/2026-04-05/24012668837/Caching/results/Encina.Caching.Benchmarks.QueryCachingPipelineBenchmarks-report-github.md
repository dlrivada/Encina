```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                              | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | concurrencyLevel | Mean         | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |----------------- |-------------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **?**                |     **10.41 μs** |  **0.216 μs** |  **0.129 μs** |  **1.00** |    **0.02** | **0.0763** | **0.0610** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |     10.48 μs |  0.034 μs |  0.018 μs |  1.01 |    0.01 | 0.0916 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |    104.57 μs |  3.404 μs |  2.025 μs | 10.04 |    0.22 | 1.2207 | 1.0986 |  37.95 KB |       19.20 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |     50.26 μs |  0.148 μs |  0.098 μs |  4.83 |    0.06 | 0.4883 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |              |             |                  |              |           |           |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 41,076.54 μs |        NA |  0.000 μs |  1.00 |    0.00 |      - |      - |   3.44 KB |        1.00 |
| Pipeline_CacheHit                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 42,580.43 μs |        NA |  0.000 μs |  1.04 |    0.00 |      - |      - |   7.33 KB |        2.13 |
| Pipeline_SequentialDifferentQueries | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 41,613.09 μs |        NA |  0.000 μs |  1.01 |    0.00 |      - |      - |  23.76 KB |        6.91 |
| Pipeline_SequentialSameQuery        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 40,622.51 μs |        NA |  0.000 μs |  0.99 |    0.00 |      - |      - |  16.96 KB |        4.93 |
|                                     |            |                |             |             |              |             |                  |              |           |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **10**               |     **79.84 μs** |  **1.823 μs** |  **0.954 μs** |     **?** |       **?** | **0.6104** | **0.2441** |  **17.62 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |              |           |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10               | 45,305.38 μs |        NA |  0.000 μs |     ? |       ? |      - |      - |  22.39 KB |           ? |
|                                     |            |                |             |             |              |             |                  |              |           |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **50**               |    **414.29 μs** | **12.861 μs** |  **6.727 μs** |     **?** |       **?** | **3.4180** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |              |           |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 50               | 47,151.03 μs |        NA |  0.000 μs |     ? |       ? |      - |      - |  96.66 KB |           ? |
|                                     |            |                |             |             |              |             |                  |              |           |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **100**              |    **826.60 μs** | **26.568 μs** | **13.896 μs** |     **?** |       **?** | **6.8359** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |              |           |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 100              | 45,661.19 μs |        NA |  0.000 μs |     ? |       ? |      - |      - | 204.91 KB |           ? |
