```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |   **239.33 μs** |      **5.755 μs** |     **3.425 μs** |     **?** |       **?** |  **1.9531** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |   229.06 μs |     12.301 μs |     0.674 μs |     ? |       ? |  1.9531 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        | **1,180.47 μs** |      **4.136 μs** |     **2.461 μs** |     **?** |       **?** |  **9.7656** |       **-** |       **-** |  **855.11 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        | 1,193.33 μs |  1,048.551 μs |    57.475 μs |     ? |       ? |  9.7656 |       - |       - |  855.07 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |    **21.68 μs** |      **0.052 μs** |     **0.027 μs** |  **1.00** |    **0.00** |  **0.1831** |       **-** |       **-** |   **17.13 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |    31.67 μs |      3.686 μs |     2.438 μs |  1.46 |    0.11 |  0.1831 |       - |       - |   19.28 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |   234.34 μs |      3.109 μs |     1.626 μs | 10.81 |    0.07 |  1.9531 |       - |       - |  169.04 KB |        9.87 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |    22.82 μs |      0.466 μs |     0.026 μs |  1.00 |    0.00 |  0.1831 |       - |       - |   16.88 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |    33.48 μs |     51.080 μs |     2.800 μs |  1.47 |    0.11 |  0.1831 |       - |       - |   19.29 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |   231.88 μs |      9.049 μs |     0.496 μs | 10.16 |    0.02 |  1.9531 |       - |       - |  169.04 KB |       10.01 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        | **3,212.61 μs** |  **1,336.710 μs** |   **884.151 μs** |     **?** |       **?** | **14.6484** | **14.6484** | **14.6484** |  **642.88 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        | 2,231.79 μs |  6,150.858 μs |   337.149 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  362.89 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       | **6,220.07 μs** |  **2,664.964 μs** | **1,762.709 μs** |     **?** |       **?** | **14.6484** | **14.6484** | **14.6484** | **1266.84 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       | 4,134.75 μs | 11,004.270 μs |   603.181 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  706.81 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **7,868.64 μs** |  **3,443.425 μs** | **2,277.613 μs** |     **?** |       **?** | **13.6719** | **13.6719** | **13.6719** | **1588.71 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 5,417.82 μs | 12,419.294 μs |   680.743 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  908.85 KB |           ? |
