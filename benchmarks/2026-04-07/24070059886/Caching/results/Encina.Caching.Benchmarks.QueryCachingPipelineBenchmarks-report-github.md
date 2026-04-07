```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                              | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | concurrencyLevel | Mean          | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |----------------- |--------------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **?**                |      **9.244 μs** |  **0.4347 μs** | **0.2587 μs** |  **1.00** |    **0.04** | **0.1068** | **0.0458** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |      9.114 μs |  0.0202 μs | 0.0105 μs |  0.99 |    0.03 | 0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |     96.597 μs |  2.5092 μs | 1.3124 μs | 10.46 |    0.31 | 1.9531 | 1.3428 |  37.97 KB |       19.21 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           | ?                |     47.325 μs |  0.3050 μs | 0.2017 μs |  5.12 |    0.14 | 0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |             |              |             |                  |               |            |           |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 41,815.191 μs |         NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |   3.44 KB |        1.00 |
| Pipeline_CacheHit                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 42,851.230 μs |         NA | 0.0000 μs |  1.02 |    0.00 |      - |      - |   7.33 KB |        2.13 |
| Pipeline_SequentialDifferentQueries | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 41,760.820 μs |         NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |  23.76 KB |        6.91 |
| Pipeline_SequentialSameQuery        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | ?                | 41,452.356 μs |         NA | 0.0000 μs |  0.99 |    0.00 |      - |      - |  16.96 KB |        4.93 |
|                                     |            |                |             |             |              |             |                  |               |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **10**               |     **72.833 μs** |  **1.3050 μs** | **0.6825 μs** |     **?** |       **?** | **0.9766** | **0.3662** |  **17.62 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |               |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10               | 46,410.794 μs |         NA | 0.0000 μs |     ? |       ? |      - |      - |  22.39 KB |           ? |
|                                     |            |                |             |             |              |             |                  |               |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **50**               |    **365.327 μs** |  **6.4052 μs** | **3.3501 μs** |     **?** |       **?** | **4.8828** | **1.4648** |  **87.47 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |               |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 50               | 47,508.631 μs |         NA | 0.0000 μs |     ? |       ? |      - |      - |  96.83 KB |           ? |
|                                     |            |                |             |             |              |             |                  |               |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **Default**     | **16**           | **3**           | **100**              |    **786.289 μs** | **18.2061 μs** | **9.5221 μs** |     **?** |       **?** | **9.7656** | **2.9297** | **174.78 KB** |           **?** |
|                                     |            |                |             |             |              |             |                  |               |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 100              | 48,031.151 μs |         NA | 0.0000 μs |     ? |       ? |      - |      - | 189.72 KB |           ? |
