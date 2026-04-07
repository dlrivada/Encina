```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.92GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | keyCount | concurrencyLevel | Mean         | Error        | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |--------- |----------------- |-------------:|-------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**        | **?**                |     **36.35 μs** |     **0.136 μs** |      **0.090 μs** |  **1.00** |    **0.00** |  **0.9766** |       **-** |       **-** |   **16.88 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | 3           | ?        | ?                |     57.37 μs |     3.748 μs |      2.231 μs |  1.58 |    0.06 |  1.1597 |       - |       - |   19.48 KB |        1.15 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | 3           | ?        | ?                |    374.75 μs |     1.793 μs |      1.067 μs | 10.31 |    0.04 | 10.2539 |       - |       - |  169.05 KB |       10.02 |
|                                 |            |                |             |             |          |                  |              |              |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | MediumRun  | 15             | 2           | 10          | ?        | ?                |     35.65 μs |     0.563 μs |      0.843 μs |  1.00 |    0.03 |  0.9766 |       - |       - |    16.9 KB |        1.00 |
| Invalidation_WithMatchingKeys   | MediumRun  | 15             | 2           | 10          | ?        | ?                |     60.18 μs |     1.841 μs |      2.756 μs |  1.69 |    0.09 |  1.1597 |       - |       - |   19.25 KB |        1.14 |
| Invalidation_SequentialCommands | MediumRun  | 15             | 2           | 10          | ?        | ?                |    389.76 μs |     1.590 μs |      2.229 μs | 10.94 |    0.26 | 10.2539 |       - |       - |  169.05 KB |       10.00 |
|                                 |            |                |             |             |          |                  |              |              |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **5**        | **?**                |  **4,868.05 μs** | **2,278.670 μs** |  **1,507.199 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.91 KB** |           **?** |
|                                 |            |                |             |             |          |                  |              |              |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | 5        | ?                |  9,379.99 μs | 1,747.319 μs |  2,615.305 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 | 1126.96 KB |           ? |
|                                 |            |                |             |             |          |                  |              |              |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**        | **10**               |    **383.72 μs** |     **1.073 μs** |      **0.561 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |             |          |                  |              |              |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | ?        | 10               |    376.45 μs |     0.686 μs |      1.027 μs |     ? |       ? | 10.2539 |       - |       - |  171.17 KB |           ? |
|                                 |            |                |             |             |          |                  |              |              |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**       | **?**                |  **4,717.07 μs** | **2,038.752 μs** |  **1,348.508 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.84 KB** |           **?** |
|                                 |            |                |             |             |          |                  |              |              |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | 10       | ?                |  9,889.65 μs | 2,241.698 μs |  3,285.851 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 | 1134.88 KB |           ? |
|                                 |            |                |             |             |          |                  |              |              |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **25**       | **?**                | **18,536.25 μs** | **6,794.853 μs** |  **4,494.375 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.86 KB** |           **?** |
|                                 |            |                |             |             |          |                  |              |              |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | 25       | ?                | 38,076.93 μs | 7,457.722 μs | 11,162.368 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 | 2751.64 KB |           ? |
|                                 |            |                |             |             |          |                  |              |              |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**        | **50**               |  **1,890.87 μs** |     **4.753 μs** |      **3.144 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.09 KB** |           **?** |
|                                 |            |                |             |             |          |                  |              |              |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | ?        | 50               |  1,889.93 μs |     2.831 μs |      3.968 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.17 KB |           ? |
