```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **346.64 μs** |      **1.276 μs** |     **0.844 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    345.20 μs |     32.282 μs |     1.770 μs |     ? |       ? | 10.2539 |       - |       - |  171.18 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,736.55 μs** |     **19.374 μs** |    **12.815 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,733.74 μs |     77.735 μs |     4.261 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.13 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **32.08 μs** |      **0.124 μs** |     **0.065 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |   **17.17 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     52.95 μs |      2.656 μs |     1.757 μs |  1.65 |    0.05 |  1.1597 |       - |       - |    19.1 KB |        1.11 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    346.15 μs |      2.145 μs |     1.419 μs | 10.79 |    0.05 | 10.2539 |       - |       - |  169.05 KB |        9.84 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     31.90 μs |      0.273 μs |     0.015 μs |  1.00 |    0.00 |  1.0376 |       - |       - |   17.07 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     51.61 μs |     27.908 μs |     1.530 μs |  1.62 |    0.04 |  1.1597 |       - |       - |   19.48 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    347.97 μs |     31.540 μs |     1.729 μs | 10.91 |    0.05 | 10.2539 |       - |       - |  169.04 KB |        9.90 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        | **11,972.63 μs** |  **8,567.421 μs** | **5,666.818 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.98 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  4,906.12 μs | 25,972.319 μs | 1,423.630 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.92 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       | **12,436.98 μs** |  **7,627.931 μs** | **5,045.404 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.89 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  4,462.06 μs | 17,694.938 μs |   969.919 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.84 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **33,774.46 μs** | **14,823.962 μs** | **9,805.133 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.88 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 15,112.95 μs | 48,653.305 μs | 2,666.852 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  938.37 KB |           ? |
