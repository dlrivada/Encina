```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |   **274.15 μs** |      **0.550 μs** |     **0.327 μs** |     **?** |       **?** |  **1.9531** |       **-** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |   275.88 μs |     41.214 μs |     2.259 μs |     ? |       ? |  1.9531 |       - |       - |  171.14 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        | **1,375.43 μs** |     **11.193 μs** |     **5.854 μs** |     **?** |       **?** |  **9.7656** |       **-** |       **-** |  **855.07 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        | 1,361.76 μs |     22.029 μs |     1.207 μs |     ? |       ? |  9.7656 |       - |       - |  855.08 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |    **27.09 μs** |      **0.124 μs** |     **0.074 μs** |  **1.00** |    **0.00** |  **0.1831** |       **-** |       **-** |   **16.97 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |    35.53 μs |      3.414 μs |     2.258 μs |  1.31 |    0.08 |  0.1831 |       - |       - |   19.13 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |   278.99 μs |      0.901 μs |     0.596 μs | 10.30 |    0.03 |  1.9531 |       - |       - |  169.03 KB |        9.96 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |    27.10 μs |      4.251 μs |     0.233 μs |  1.00 |    0.01 |  0.1831 |       - |       - |   16.83 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |    41.12 μs |     45.877 μs |     2.515 μs |  1.52 |    0.08 |  0.1831 |       - |       - |   19.13 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |   277.19 μs |     10.838 μs |     0.594 μs | 10.23 |    0.08 |  1.9531 |       - |       - |  169.03 KB |       10.04 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        | **3,758.37 μs** |  **1,574.570 μs** | **1,041.481 μs** |     **?** |       **?** | **14.6484** | **14.6484** | **14.6484** |  **646.93 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        | 2,558.13 μs |  6,005.381 μs |   329.175 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  366.94 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       | **7,332.38 μs** |  **3,250.398 μs** | **2,149.937 μs** |     **?** |       **?** | **14.6484** | **14.6484** | **14.6484** | **1274.88 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       | 4,730.33 μs | 12,797.617 μs |   701.481 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  714.88 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **9,088.97 μs** |  **3,809.975 μs** | **2,520.062 μs** |     **?** |       **?** | **13.6719** | **13.6719** | **13.6719** | **1608.86 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 6,181.99 μs | 14,881.994 μs |   815.732 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  908.87 KB |           ? |
