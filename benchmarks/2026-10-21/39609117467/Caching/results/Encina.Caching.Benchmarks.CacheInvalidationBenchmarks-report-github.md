```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **418.08 μs** |      **1.479 μs** |     **0.880 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **173.82 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    415.44 μs |     21.621 μs |     1.185 μs |     ? |       ? | 10.2539 |       - |       - |  173.82 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **2,089.82 μs** |      **6.813 μs** |     **4.506 μs** |     **?** |       **?** | **50.7813** |       **-** |       **-** |  **868.45 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  2,107.94 μs |    151.884 μs |     8.325 μs |     ? |       ? | 50.7813 |       - |       - |  868.53 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **38.61 μs** |      **0.061 μs** |     **0.040 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |   **17.25 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     64.71 μs |     10.244 μs |     6.776 μs |  1.68 |    0.17 |  1.0986 |       - |       - |   19.53 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    413.43 μs |      1.388 μs |     0.726 μs | 10.71 |    0.02 | 10.2539 |       - |       - |  171.71 KB |        9.95 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     39.22 μs |      2.139 μs |     0.117 μs |  1.00 |    0.00 |  1.0376 |       - |       - |   17.38 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     66.60 μs |    174.252 μs |     9.551 μs |  1.70 |    0.21 |  1.0986 |       - |       - |   19.53 KB |        1.12 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    419.60 μs |     12.739 μs |     0.698 μs | 10.70 |    0.03 | 10.2539 |       - |       - |  171.71 KB |        9.88 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **6,793.15 μs** |  **4,167.113 μs** | **2,756.287 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **647.22 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,472.61 μs |  8,063.343 μs |   441.979 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |   367.2 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,469.16 μs** |  **2,736.005 μs** | **1,809.698 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **655.18 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,253.28 μs |  7,352.890 μs |   403.037 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  375.17 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **16,571.94 μs** |  **7,552.326 μs** | **4,995.396 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1609.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  8,283.98 μs | 26,789.467 μs | 1,468.421 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  909.11 KB |           ? |
