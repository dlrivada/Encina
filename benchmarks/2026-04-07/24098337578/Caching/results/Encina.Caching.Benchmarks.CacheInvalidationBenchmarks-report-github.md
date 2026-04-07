```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               | **?**        |    **349.39 μs** |      **3.025 μs** |      **2.001 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.14 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | 10               | ?        |    347.14 μs |      1.320 μs |      1.935 μs |     ? |       ? | 10.2539 |       - |       - |  171.14 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **?**        |  **1,749.76 μs** |     **11.246 μs** |      **7.439 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.13 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | 50               | ?        |  1,719.72 μs |      5.680 μs |      8.326 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.08 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **?**        |     **32.96 μs** |      **0.281 μs** |      **0.186 μs** |  **1.00** |    **0.01** |  **0.9766** |       **-** |       **-** |    **16.9 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | 3           | ?                | ?        |     51.46 μs |      1.603 μs |      0.954 μs |  1.56 |    0.03 |  1.1597 |       - |       - |   19.25 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | 3           | ?                | ?        |    345.04 μs |      3.469 μs |      2.295 μs | 10.47 |    0.09 | 10.2539 |       - |       - |  169.04 KB |       10.00 |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | MediumRun  | 15             | 2           | 10          | ?                | ?        |     33.48 μs |      0.174 μs |      0.260 μs |  1.00 |    0.01 |  0.9766 |       - |       - |   16.93 KB |        1.00 |
| Invalidation_WithMatchingKeys   | MediumRun  | 15             | 2           | 10          | ?                | ?        |     51.06 μs |      1.023 μs |      1.532 μs |  1.53 |    0.05 |  1.1597 |       - |       - |   19.35 KB |        1.14 |
| Invalidation_SequentialCommands | MediumRun  | 15             | 2           | 10          | ?                | ?        |    345.47 μs |      2.090 μs |      3.064 μs | 10.32 |    0.12 | 10.2539 |       - |       - |  169.07 KB |        9.99 |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **5**        |  **5,263.06 μs** |  **2,532.679 μs** |  **1,675.210 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.93 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 5        | 11,848.67 μs |  2,373.481 μs |  3,479.018 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 | 1126.92 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **10**       |  **5,064.69 μs** |  **2,413.745 μs** |  **1,596.543 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.86 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 10       | 13,795.19 μs |  4,032.937 μs |  6,036.311 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 | 1134.88 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **25**       | **18,092.88 μs** | **11,342.237 μs** |  **7,502.188 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.85 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 25       | 44,953.64 μs |  8,470.395 μs | 12,678.090 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 | 1338.98 KB |           ? |
