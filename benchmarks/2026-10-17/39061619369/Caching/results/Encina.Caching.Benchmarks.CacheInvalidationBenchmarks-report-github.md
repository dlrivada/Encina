```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **412.92 μs** |      **1.849 μs** |     **1.223 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **173.81 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    413.55 μs |      7.645 μs |     0.419 μs |     ? |       ? | 10.2539 |       - |       - |   173.8 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **2,005.33 μs** |      **8.595 μs** |     **5.685 μs** |     **?** |       **?** | **50.7813** |       **-** |       **-** |  **868.43 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  2,037.84 μs |    104.029 μs |     5.702 μs |     ? |       ? | 50.7813 |       - |       - |  868.38 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **38.32 μs** |      **0.245 μs** |     **0.146 μs** |  **1.00** |    **0.01** |  **1.0376** |       **-** |       **-** |   **17.07 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     66.93 μs |     11.384 μs |     7.530 μs |  1.75 |    0.19 |  1.0986 |       - |       - |    19.4 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    408.13 μs |      2.045 μs |     1.352 μs | 10.65 |    0.05 | 10.2539 |       - |       - |  171.71 KB |       10.06 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     39.86 μs |      1.704 μs |     0.093 μs |  1.00 |    0.00 |  1.0376 |       - |       - |   17.14 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     71.71 μs |    151.293 μs |     8.293 μs |  1.80 |    0.18 |  1.0986 |       - |       - |   19.45 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    404.92 μs |     23.481 μs |     1.287 μs | 10.16 |    0.03 | 10.2539 |       - |       - |   171.7 KB |       10.02 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **4,809.28 μs** |  **2,186.462 μs** | **1,446.209 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **647.22 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,191.70 μs |  7,915.728 μs |   433.888 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  367.19 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **4,994.29 μs** |  **2,433.309 μs** | **1,609.483 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **655.17 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,188.96 μs |  8,026.415 μs |   439.955 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  375.17 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **15,980.52 μs** | **10,003.576 μs** | **6,616.746 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1609.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  9,666.71 μs | 35,423.613 μs | 1,941.688 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |   909.1 KB |           ? |
