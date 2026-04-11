```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | keyCount | Mean         | Error        | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------------- |--------- |-------------:|-------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               | **?**        |    **380.44 μs** |     **1.458 μs** |      **0.868 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |              |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | 10               | ?        |    382.79 μs |     1.418 μs |      2.078 μs |     ? |       ? | 10.2539 |       - |       - |  171.14 KB |           ? |
|                                 |            |                |             |             |                  |          |              |              |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **?**        |  **1,910.98 μs** |     **7.485 μs** |      **4.454 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.17 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |              |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | 50               | ?        |  1,890.18 μs |     8.443 μs |     12.108 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.15 KB |           ? |
|                                 |            |                |             |             |                  |          |              |              |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **?**        |     **35.32 μs** |     **0.387 μs** |      **0.256 μs** |  **1.00** |    **0.01** |  **1.0376** |       **-** |       **-** |      **17 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | 3           | ?                | ?        |     62.14 μs |     7.682 μs |      5.081 μs |  1.76 |    0.14 |  1.0986 |       - |       - |   19.36 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | 3           | ?                | ?        |    382.50 μs |     1.678 μs |      1.110 μs | 10.83 |    0.08 | 10.2539 |       - |       - |  169.05 KB |        9.94 |
|                                 |            |                |             |             |                  |          |              |              |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | MediumRun  | 15             | 2           | 10          | ?                | ?        |     35.20 μs |     0.332 μs |      0.486 μs |  1.00 |    0.02 |  1.0376 |       - |       - |   17.07 KB |        1.00 |
| Invalidation_WithMatchingKeys   | MediumRun  | 15             | 2           | 10          | ?                | ?        |     60.04 μs |     2.758 μs |      4.043 μs |  1.71 |    0.12 |  1.0986 |       - |       - |   19.23 KB |        1.13 |
| Invalidation_SequentialCommands | MediumRun  | 15             | 2           | 10          | ?                | ?        |    382.43 μs |     2.552 μs |      3.493 μs | 10.87 |    0.18 | 10.2539 |       - |       - |  169.04 KB |        9.90 |
|                                 |            |                |             |             |                  |          |              |              |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **5**        |  **5,121.11 μs** | **2,826.791 μs** |  **1,869.747 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.92 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |              |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 5        | 10,399.50 μs | 1,972.969 μs |  2,953.047 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 | 1126.98 KB |           ? |
|                                 |            |                |             |             |                  |          |              |              |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **10**       |  **4,616.88 μs** | **2,065.916 μs** |  **1,366.475 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.84 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |              |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 10       | 14,378.44 μs | 5,138.687 μs |  7,532.221 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 | 1134.88 KB |           ? |
|                                 |            |                |             |             |                  |          |              |              |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **25**       | **26,003.59 μs** | **9,910.452 μs** |  **6,555.150 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.86 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |              |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 25       | 38,467.39 μs | 7,208.931 μs | 10,789.989 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 | 1644.43 KB |           ? |
