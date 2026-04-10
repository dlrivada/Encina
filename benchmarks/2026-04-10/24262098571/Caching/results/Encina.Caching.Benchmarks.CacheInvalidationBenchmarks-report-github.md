```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **387.56 μs** |      **1.196 μs** |     **0.791 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** | **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    378.24 μs |     26.285 μs |     1.441 μs |     ? |       ? | 10.2539 |       - |       - | 171.15 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,912.77 μs** |      **7.339 μs** |     **3.838 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** | **855.21 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,890.55 μs |     43.939 μs |     2.408 μs |     ? |       ? | 50.7813 |  1.9531 |       - | 855.14 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **35.57 μs** |      **0.152 μs** |     **0.090 μs** |  **1.00** |    **0.00** |  **0.9766** |       **-** |       **-** |  **16.89 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     58.98 μs |      3.984 μs |     2.371 μs |  1.66 |    0.06 |  1.1597 |  0.0610 |       - |  19.29 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    379.91 μs |      1.512 μs |     0.900 μs | 10.68 |    0.04 | 10.2539 |       - |       - | 169.04 KB |       10.01 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     36.05 μs |      1.274 μs |     0.070 μs |  1.00 |    0.00 |  0.9766 |       - |       - |  16.91 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     64.88 μs |    180.817 μs |     9.911 μs |  1.80 |    0.24 |  1.0986 |       - |       - |  19.11 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    386.58 μs |      7.686 μs |     0.421 μs | 10.72 |    0.02 | 10.2539 |       - |       - | 169.04 KB |       10.00 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,658.36 μs** |  **2,679.098 μs** | **1,772.058 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **646.91 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,098.85 μs |  7,695.217 μs |   421.801 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 | 366.91 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,395.78 μs** |  **3,514.720 μs** | **2,324.770 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **654.84 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,260.60 μs |  9,126.534 μs |   500.256 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 | 374.85 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **20,644.10 μs** | **11,233.759 μs** | **7,430.436 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.9 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  9,713.94 μs | 32,558.825 μs | 1,784.659 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 | 908.83 KB |           ? |
