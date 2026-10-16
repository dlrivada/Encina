```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |   **193.41 μs** |      **3.386 μs** |     **2.240 μs** |     **?** |       **?** | **10.2539** |  **0.2441** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |   193.99 μs |    127.371 μs |     6.982 μs |     ? |       ? | 10.2539 |  0.2441 |       - |  171.15 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |   **958.54 μs** |     **10.186 μs** |     **6.062 μs** |     **?** |       **?** | **51.7578** |  **2.9297** |       **-** |  **855.17 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |   936.62 μs |      8.028 μs |     0.440 μs |     ? |       ? | 51.7578 |  2.9297 |       - |  855.19 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |    **17.53 μs** |      **0.166 μs** |     **0.099 μs** |  **1.00** |    **0.01** |  **1.0071** |  **0.0305** |       **-** |   **16.92 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |    29.83 μs |      1.087 μs |     0.719 μs |  1.70 |    0.04 |  1.1597 |  0.0305 |       - |   19.05 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |   187.29 μs |      2.854 μs |     1.698 μs | 10.69 |    0.11 | 10.2539 |  0.2441 |       - |  169.05 KB |        9.99 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |    17.18 μs |      4.808 μs |     0.264 μs |  1.00 |    0.02 |  1.0071 |  0.0305 |       - |   16.91 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |    26.80 μs |     23.208 μs |     1.272 μs |  1.56 |    0.07 |  1.1597 |  0.0305 |       - |   19.24 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |   181.30 μs |     24.062 μs |     1.319 μs | 10.55 |    0.16 | 10.2539 |  0.2441 |       - |  169.05 KB |       10.00 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        | **6,784.60 μs** |  **4,009.917 μs** | **2,652.312 μs** |     **?** |       **?** | **17.5781** | **16.1133** | **16.1133** | **1262.91 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        | 3,638.11 μs | 11,102.363 μs |   608.558 μs |     ? |       ? | 17.0898 | 15.6250 | 15.6250 |  702.91 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       | **8,115.31 μs** |  **4,582.203 μs** | **3,030.843 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1266.82 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       | 4,057.37 μs | 13,582.504 μs |   744.503 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  706.82 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **9,932.82 μs** |  **7,127.618 μs** | **4,714.478 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1588.73 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 5,125.62 μs | 15,083.624 μs |   826.784 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  888.72 KB |           ? |
