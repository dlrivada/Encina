```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **393.26 μs** |      **3.449 μs** |      **2.281 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    378.17 μs |     41.622 μs |      2.281 μs |     ? |       ? | 10.2539 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,915.89 μs** |      **3.265 μs** |      **1.943 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.14 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,916.88 μs |     19.092 μs |      1.047 μs |     ? |       ? | 50.7813 |  1.9531 |       - |   855.2 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **35.79 μs** |      **0.071 μs** |      **0.043 μs** |  **1.00** |    **0.00** |  **0.9766** |       **-** |       **-** |   **16.92 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     57.45 μs |      5.757 μs |      3.808 μs |  1.60 |    0.10 |  1.1597 |       - |       - |   19.33 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    378.37 μs |      2.035 μs |      1.346 μs | 10.57 |    0.04 | 10.2539 |       - |       - |  169.05 KB |        9.99 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     35.66 μs |      1.240 μs |      0.068 μs |  1.00 |    0.00 |  1.0376 |       - |       - |      17 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     58.24 μs |     42.194 μs |      2.313 μs |  1.63 |    0.06 |  1.1597 |       - |       - |   19.27 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    380.36 μs |     13.935 μs |      0.764 μs | 10.67 |    0.03 | 10.2539 |       - |       - |  169.04 KB |        9.94 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,571.18 μs** |  **2,836.816 μs** |  **1,876.378 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.95 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,345.54 μs |  7,698.453 μs |    421.978 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.89 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,490.15 μs** |  **3,248.073 μs** |  **2,148.399 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.86 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,341.39 μs |  8,352.311 μs |    457.818 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.88 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **23,363.52 μs** | **15,303.077 μs** | **10,122.038 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.84 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 12,751.56 μs | 37,391.732 μs |  2,049.567 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  938.33 KB |           ? |
