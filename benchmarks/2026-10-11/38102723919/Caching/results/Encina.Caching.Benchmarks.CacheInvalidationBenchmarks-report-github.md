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
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               | **?**        |    **381.30 μs** |      **1.803 μs** |      **1.192 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | 10               | ?        |    377.07 μs |      0.656 μs |      0.962 μs |     ? |       ? | 10.2539 |       - |       - |  171.15 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **?**        |  **1,885.18 μs** |      **6.153 μs** |      **3.662 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.14 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | 50               | ?        |  1,892.32 μs |      6.143 μs |      9.004 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.18 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **?**        |     **35.21 μs** |      **0.097 μs** |      **0.058 μs** |  **1.00** |    **0.00** |  **0.9766** |       **-** |       **-** |   **16.91 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | 3           | ?                | ?        |     59.59 μs |      5.991 μs |      3.962 μs |  1.69 |    0.11 |  1.1597 |       - |       - |   19.27 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | 3           | ?                | ?        |    374.44 μs |      1.727 μs |      1.142 μs | 10.63 |    0.04 | 10.2539 |       - |       - |  169.05 KB |       10.00 |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | MediumRun  | 15             | 2           | 10          | ?                | ?        |     35.50 μs |      0.079 μs |      0.113 μs |  1.00 |    0.00 |  0.9766 |       - |       - |   16.89 KB |        1.00 |
| Invalidation_WithMatchingKeys   | MediumRun  | 15             | 2           | 10          | ?                | ?        |     55.70 μs |      2.657 μs |      3.895 μs |  1.57 |    0.11 |  1.0986 |       - |       - |   19.28 KB |        1.14 |
| Invalidation_SequentialCommands | MediumRun  | 15             | 2           | 10          | ?                | ?        |    379.68 μs |      0.982 μs |      1.376 μs | 10.70 |    0.05 | 10.2539 |       - |       - |  169.06 KB |       10.01 |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **5**        |  **4,890.30 μs** |  **2,221.539 μs** |  **1,469.410 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.93 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 5        | 10,963.89 μs |  2,340.625 μs |  3,503.338 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 | 1126.97 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **10**       |  **4,874.40 μs** |  **2,183.266 μs** |  **1,444.095 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.84 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 10       | 11,510.98 μs |  2,259.154 μs |  3,381.395 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 | 1134.89 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **25**       | **16,823.41 μs** | **11,098.816 μs** |  **7,341.180 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.87 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 25       | 40,626.65 μs |  8,359.786 μs | 12,512.535 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 | 1527.44 KB |           ? |
