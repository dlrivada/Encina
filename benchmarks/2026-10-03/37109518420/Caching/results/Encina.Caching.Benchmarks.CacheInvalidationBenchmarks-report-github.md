```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **378.95 μs** |      **1.423 μs** |     **0.847 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.17 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    385.23 μs |     92.232 μs |     5.056 μs |     ? |       ? | 10.2539 |       - |       - |  171.14 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,892.67 μs** |     **12.595 μs** |     **7.495 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.13 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,873.89 μs |     56.706 μs |     3.108 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.17 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **36.05 μs** |      **0.121 μs** |     **0.072 μs** |  **1.00** |    **0.00** |  **0.9766** |       **-** |       **-** |   **16.95 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     61.32 μs |      5.258 μs |     3.478 μs |  1.70 |    0.09 |  1.1597 |  0.0610 |       - |   19.22 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    379.39 μs |      1.567 μs |     1.036 μs | 10.52 |    0.03 | 10.2539 |       - |       - |  169.06 KB |        9.98 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     36.07 μs |      1.707 μs |     0.094 μs |  1.00 |    0.00 |  0.9766 |       - |       - |   16.95 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     59.82 μs |     16.398 μs |     0.899 μs |  1.66 |    0.02 |  1.1597 |       - |       - |   19.08 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    379.70 μs |      2.394 μs |     0.131 μs | 10.53 |    0.02 | 10.2539 |       - |       - |  169.04 KB |        9.98 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **4,871.58 μs** |  **2,178.600 μs** | **1,441.009 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.91 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,024.61 μs |  7,407.806 μs |   406.047 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.93 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **4,614.98 μs** |  **2,209.930 μs** | **1,461.732 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.85 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,156.34 μs |  7,976.111 μs |   437.198 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.87 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **18,221.31 μs** | **11,922.385 μs** | **7,885.920 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.86 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  8,627.59 μs | 33,976.109 μs | 1,862.345 μs |     ? |       ? | 19.5313 | 17.5781 | 17.5781 |  938.03 KB |           ? |
