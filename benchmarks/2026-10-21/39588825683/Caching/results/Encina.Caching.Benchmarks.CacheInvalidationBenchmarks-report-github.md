```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **421.57 μs** |      **3.027 μs** |     **2.002 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **173.82 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    409.80 μs |     27.191 μs |     1.490 μs |     ? |       ? | 10.2539 |       - |       - |  173.81 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **2,017.93 μs** |      **6.926 μs** |     **3.622 μs** |     **?** |       **?** | **50.7813** |       **-** |       **-** |  **868.34 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  2,064.44 μs |    183.510 μs |    10.059 μs |     ? |       ? | 50.7813 |       - |       - |  868.35 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **38.33 μs** |      **0.479 μs** |     **0.285 μs** |  **1.00** |    **0.01** |  **1.0376** |       **-** |       **-** |   **17.34 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     57.53 μs |      4.578 μs |     3.028 μs |  1.50 |    0.08 |  1.0986 |       - |       - |   19.62 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    410.57 μs |      2.681 μs |     1.773 μs | 10.71 |    0.09 | 10.2539 |       - |       - |  171.74 KB |        9.91 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     39.07 μs |      0.282 μs |     0.015 μs |  1.00 |    0.00 |  1.0376 |       - |       - |   17.07 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     63.78 μs |     61.816 μs |     3.388 μs |  1.63 |    0.08 |  1.1597 |       - |       - |   19.42 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    412.06 μs |     21.551 μs |     1.181 μs | 10.55 |    0.03 | 10.2539 |       - |       - |   171.7 KB |       10.06 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **4,863.53 μs** |  **2,237.029 μs** | **1,479.656 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |   **647.2 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,252.95 μs | 10,504.604 μs |   575.793 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  367.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **4,832.04 μs** |  **2,281.134 μs** | **1,508.829 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **655.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,290.30 μs |  8,623.815 μs |   472.700 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  375.14 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **14,981.18 μs** |  **8,551.627 μs** | **5,656.372 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1609.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  8,751.97 μs | 32,155.630 μs | 1,762.559 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  909.12 KB |           ? |
