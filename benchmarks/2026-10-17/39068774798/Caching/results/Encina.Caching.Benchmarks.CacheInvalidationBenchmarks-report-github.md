```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **284.76 μs** |      **1.303 μs** |      **0.775 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **173.83 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    288.68 μs |     25.422 μs |      1.393 μs |     ? |       ? | 10.2539 |       - |       - |  173.82 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,420.28 μs** |      **9.339 μs** |      **6.177 μs** |     **?** |       **?** | **52.7344** |  **1.9531** |       **-** |  **868.44 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,428.54 μs |    115.617 μs |      6.337 μs |     ? |       ? | 52.7344 |  1.9531 |       - |  868.49 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **27.75 μs** |      **0.186 μs** |      **0.123 μs** |  **1.00** |    **0.01** |  **1.0071** |  **0.0305** |       **-** |   **16.92 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     43.07 μs |      3.690 μs |      2.441 μs |  1.55 |    0.08 |  1.1597 |  0.0610 |       - |   19.33 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    283.78 μs |      1.849 μs |      1.100 μs | 10.23 |    0.06 | 10.2539 |       - |       - |   171.7 KB |       10.15 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     27.52 μs |      3.449 μs |      0.189 μs |  1.00 |    0.01 |  1.0071 |  0.0305 |       - |    16.9 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     39.84 μs |     46.700 μs |      2.560 μs |  1.45 |    0.08 |  1.1597 |       - |       - |   19.52 KB |        1.15 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    282.45 μs |     13.863 μs |      0.760 μs | 10.26 |    0.07 | 10.2539 |       - |       - |   171.7 KB |       10.16 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,820.15 μs** |  **2,830.225 μs** |  **1,684.221 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **647.22 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  4,928.66 μs | 13,938.137 μs |    763.996 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  367.17 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       | **16,799.28 μs** | **14,879.564 μs** |  **9,841.910 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1275.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  8,888.68 μs | 70,009.534 μs |  3,837.459 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  715.15 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **27,957.39 μs** | **15,439.750 μs** | **10,212.439 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1609.12 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 10,021.48 μs | 42,488.727 μs |  2,328.951 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  909.11 KB |           ? |
