```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               | **?**        |    **344.46 μs** |      **2.629 μs** |      **1.739 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | 10               | ?        |    344.15 μs |      1.148 μs |      1.718 μs |     ? |       ? | 10.2539 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **?**        |  **1,720.43 μs** |      **6.871 μs** |      **3.594 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.13 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | 50               | ?        |  1,714.84 μs |      4.500 μs |      6.596 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.15 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **?**        |     **33.59 μs** |      **0.193 μs** |      **0.127 μs** |  **1.00** |    **0.01** |  **0.9766** |       **-** |       **-** |   **16.72 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | 3           | ?                | ?        |     51.58 μs |      1.423 μs |      0.847 μs |  1.54 |    0.02 |  1.1597 |       - |       - |    19.2 KB |        1.15 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | 3           | ?                | ?        |    343.46 μs |      2.905 μs |      1.922 μs | 10.22 |    0.07 | 10.2539 |       - |       - |  169.04 KB |       10.11 |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | MediumRun  | 15             | 2           | 10          | ?                | ?        |     31.99 μs |      0.337 μs |      0.504 μs |  1.00 |    0.02 |  1.0376 |       - |       - |   16.98 KB |        1.00 |
| Invalidation_WithMatchingKeys   | MediumRun  | 15             | 2           | 10          | ?                | ?        |     51.79 μs |      0.802 μs |      1.176 μs |  1.62 |    0.04 |  1.1597 |       - |       - |   19.13 KB |        1.13 |
| Invalidation_SequentialCommands | MediumRun  | 15             | 2           | 10          | ?                | ?        |    341.92 μs |      0.756 μs |      1.060 μs | 10.69 |    0.17 | 10.2539 |       - |       - |  169.04 KB |        9.96 |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **5**        |  **5,065.91 μs** |  **2,334.742 μs** |  **1,544.287 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.89 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 5        | 10,711.97 μs |  2,027.327 μs |  3,034.409 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 | 1126.93 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **10**       |  **4,990.00 μs** |  **2,199.008 μs** |  **1,454.508 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.88 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 10       | 10,969.84 μs |  2,153.107 μs |  3,222.670 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 | 1134.89 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **25**       | **17,827.13 μs** | **11,868.251 μs** |  **7,850.113 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.85 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 25       | 44,396.45 μs |  8,626.082 μs | 12,911.114 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 | 1557.21 KB |           ? |
