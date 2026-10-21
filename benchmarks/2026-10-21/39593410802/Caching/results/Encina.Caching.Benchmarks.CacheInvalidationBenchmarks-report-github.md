```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **419.92 μs** |      **3.306 μs** |      **1.967 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |   **173.8 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    425.03 μs |     27.213 μs |      1.492 μs |     ? |       ? | 10.2539 |       - |       - |   173.8 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **2,150.44 μs** |     **14.000 μs** |      **9.260 μs** |     **?** |       **?** | **50.7813** |       **-** |       **-** |  **868.42 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  2,062.68 μs |     28.601 μs |      1.568 μs |     ? |       ? | 50.7813 |       - |       - |  868.44 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **39.54 μs** |      **0.183 μs** |      **0.121 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |   **17.24 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     64.00 μs |      7.626 μs |      5.044 μs |  1.62 |    0.12 |  1.0986 |       - |       - |   19.33 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    412.49 μs |      2.490 μs |      1.482 μs | 10.43 |    0.05 | 10.2539 |       - |       - |  171.71 KB |        9.96 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     37.99 μs |      2.606 μs |      0.143 μs |  1.00 |    0.00 |  1.0376 |       - |       - |   17.38 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     62.24 μs |     62.210 μs |      3.410 μs |  1.64 |    0.08 |  1.0986 |       - |       - |   19.44 KB |        1.12 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    411.94 μs |     22.753 μs |      1.247 μs | 10.84 |    0.05 | 10.2539 |       - |       - |   171.7 KB |        9.88 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,304.87 μs** |  **2,748.218 μs** |  **1,817.776 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **647.22 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,346.34 μs |  9,405.521 μs |    515.548 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |   367.2 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **6,580.92 μs** |  **3,796.406 μs** |  **2,511.087 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **655.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  4,234.95 μs | 16,940.351 μs |    928.558 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  375.15 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **31,381.03 μs** | **17,988.981 μs** | **11,898.597 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1609.17 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 16,594.52 μs | 70,210.016 μs |  3,848.448 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  938.39 KB |           ? |
