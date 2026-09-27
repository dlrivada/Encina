```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               | **?**        |    **380.25 μs** |      **2.410 μs** |      **1.594 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | 10               | ?        |    384.74 μs |      5.183 μs |      7.434 μs |     ? |       ? | 10.2539 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **?**        |  **1,917.83 μs** |      **9.711 μs** |      **5.779 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.16 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | 50               | ?        |  1,900.94 μs |      2.003 μs |      2.741 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.08 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **?**        |     **36.45 μs** |      **0.297 μs** |      **0.196 μs** |  **1.00** |    **0.01** |  **0.9766** |       **-** |       **-** |   **16.71 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | 3           | ?                | ?        |     60.80 μs |      6.591 μs |      4.360 μs |  1.67 |    0.11 |  1.0986 |       - |       - |   19.21 KB |        1.15 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | 3           | ?                | ?        |    381.33 μs |      2.841 μs |      1.879 μs | 10.46 |    0.07 | 10.2539 |       - |       - |  169.05 KB |       10.12 |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | MediumRun  | 15             | 2           | 10          | ?                | ?        |     35.84 μs |      0.104 μs |      0.152 μs |  1.00 |    0.01 |  1.0376 |       - |       - |   17.02 KB |        1.00 |
| Invalidation_WithMatchingKeys   | MediumRun  | 15             | 2           | 10          | ?                | ?        |     59.21 μs |      2.531 μs |      3.710 μs |  1.65 |    0.10 |  1.1597 |       - |       - |   19.34 KB |        1.14 |
| Invalidation_SequentialCommands | MediumRun  | 15             | 2           | 10          | ?                | ?        |    383.68 μs |      1.479 μs |      2.121 μs | 10.71 |    0.07 | 10.2539 |       - |       - |  169.05 KB |        9.93 |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **5**        |  **5,041.11 μs** |  **2,309.024 μs** |  **1,527.276 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.93 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 5        | 13,386.35 μs |  3,667.959 μs |  5,490.029 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 | 1126.97 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **10**       |  **4,883.62 μs** |  **2,367.641 μs** |  **1,566.048 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.88 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 10       | 12,912.16 μs |  3,396.703 μs |  5,084.025 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 | 1134.88 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **25**       | **19,047.88 μs** | **13,086.817 μs** |  **8,656.119 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.88 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 25       | 44,662.57 μs |  7,125.867 μs | 10,665.663 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 | 1437.17 KB |           ? |
