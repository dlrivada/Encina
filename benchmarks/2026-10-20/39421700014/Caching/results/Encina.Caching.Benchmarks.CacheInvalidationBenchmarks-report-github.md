```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |   **184.65 μs** |      **1.694 μs** |     **0.886 μs** |     **?** |       **?** | **10.4980** |  **0.2441** |       **-** |   **173.8 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |   184.96 μs |     34.200 μs |     1.875 μs |     ? |       ? | 10.4980 |  0.2441 |       - |  173.81 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |   **921.16 μs** |     **10.852 μs** |     **5.676 μs** |     **?** |       **?** | **52.7344** |  **2.9297** |       **-** |  **868.43 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |   910.35 μs |     44.918 μs |     2.462 μs |     ? |       ? | 52.7344 |  2.9297 |       - |  868.48 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |    **17.70 μs** |      **0.268 μs** |     **0.159 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |   **17.23 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |    30.40 μs |      2.907 μs |     1.923 μs |  1.72 |    0.10 |  1.1597 |       - |       - |   19.42 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |   182.46 μs |      3.510 μs |     2.322 μs | 10.31 |    0.15 | 10.4980 |  0.2441 |       - |  171.71 KB |        9.97 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |    16.54 μs |      0.937 μs |     0.051 μs |  1.00 |    0.00 |  1.0681 |  0.0305 |       - |   17.57 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |    28.83 μs |      2.664 μs |     0.146 μs |  1.74 |    0.01 |  1.1902 |  0.0305 |       - |   19.49 KB |        1.11 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |   183.78 μs |     24.393 μs |     1.337 μs | 11.11 |    0.08 | 10.4980 |  0.2441 |       - |  171.71 KB |        9.77 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        | **6,363.92 μs** |  **3,764.896 μs** | **2,490.246 μs** |     **?** |       **?** | **17.5781** | **16.1133** | **16.1133** | **1263.17 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        | 3,402.51 μs | 10,132.846 μs |   555.415 μs |     ? |       ? | 17.0898 | 15.6250 | 15.6250 |  703.18 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       | **6,426.55 μs** |  **3,623.923 μs** | **2,397.001 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1267.07 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       | 3,692.34 μs | 12,063.739 μs |   661.254 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  715.15 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **9,213.66 μs** |  **5,862.948 μs** | **3,877.977 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1609.16 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 4,944.90 μs | 14,991.413 μs |   821.730 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  938.13 KB |           ? |
