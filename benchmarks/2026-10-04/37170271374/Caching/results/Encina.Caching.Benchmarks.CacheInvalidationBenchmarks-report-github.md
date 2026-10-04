```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | keyCount | Mean         | Error        | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------------- |--------- |-------------:|-------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               | **?**        |    **228.94 μs** |     **3.121 μs** |     **2.064 μs** |     **?** |       **?** |  **1.9531** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |              |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | 10               | ?        |    232.38 μs |     2.656 μs |     3.724 μs |     ? |       ? |  1.9531 |       - |       - |  171.15 KB |           ? |
|                                 |            |                |             |             |                  |          |              |              |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **?**        |  **1,153.20 μs** |     **7.455 μs** |     **4.931 μs** |     **?** |       **?** |  **9.7656** |       **-** |       **-** |  **855.07 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |              |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | 50               | ?        |  1,158.36 μs |     6.493 μs |     9.103 μs |     ? |       ? |  9.7656 |       - |       - |  855.15 KB |           ? |
|                                 |            |                |             |             |                  |          |              |              |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **?**        |     **22.50 μs** |     **1.178 μs** |     **0.779 μs** |  **1.00** |    **0.05** |  **0.1831** |       **-** |       **-** |   **16.86 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | 3           | ?                | ?        |     34.03 μs |     2.842 μs |     1.880 μs |  1.51 |    0.09 |  0.1831 |       - |       - |   19.32 KB |        1.15 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | 3           | ?                | ?        |    231.97 μs |     2.658 μs |     1.390 μs | 10.32 |    0.34 |  1.9531 |       - |       - |  169.04 KB |       10.03 |
|                                 |            |                |             |             |                  |          |              |              |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | MediumRun  | 15             | 2           | 10          | ?                | ?        |     22.82 μs |     0.284 μs |     0.416 μs |  1.00 |    0.03 |  0.1831 |       - |       - |   16.69 KB |        1.00 |
| Invalidation_WithMatchingKeys   | MediumRun  | 15             | 2           | 10          | ?                | ?        |     32.04 μs |     1.539 μs |     2.303 μs |  1.40 |    0.10 |  0.1831 |       - |       - |   19.09 KB |        1.14 |
| Invalidation_SequentialCommands | MediumRun  | 15             | 2           | 10          | ?                | ?        |    238.51 μs |     3.824 μs |     5.724 μs | 10.46 |    0.31 |  1.9531 |       - |       - |  169.13 KB |       10.13 |
|                                 |            |                |             |             |                  |          |              |              |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **5**        |  **3,170.00 μs** | **1,330.709 μs** |   **880.182 μs** |     **?** |       **?** | **14.6484** | **14.6484** | **14.6484** |   **642.9 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |              |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 5        |  6,041.87 μs |   878.955 μs | 1,315.578 μs |     ? |       ? | 14.6484 | 14.6484 | 14.6484 | 1122.89 KB |           ? |
|                                 |            |                |             |             |                  |          |              |              |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **10**       |  **6,082.49 μs** | **2,538.922 μs** | **1,679.339 μs** |     **?** |       **?** | **14.6484** | **14.6484** | **14.6484** | **1266.82 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |              |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 10       | 12,069.50 μs | 1,885.598 μs | 2,822.274 μs |     ? |       ? | 14.6484 | 14.6484 | 14.6484 | 2226.83 KB |           ? |
|                                 |            |                |             |             |                  |          |              |              |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **25**       |  **7,849.31 μs** | **3,547.756 μs** | **2,346.621 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **1588.67 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |              |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 25       | 14,989.91 μs | 2,393.095 μs | 3,581.872 μs |     ? |       ? | 13.6719 | 13.6719 | 13.6719 | 2788.73 KB |           ? |
