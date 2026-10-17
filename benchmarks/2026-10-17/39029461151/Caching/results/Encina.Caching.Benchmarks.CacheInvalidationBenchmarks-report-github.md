```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **414.72 μs** |      **1.817 μs** |     **1.202 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **173.83 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    407.33 μs |      5.476 μs |     0.300 μs |     ? |       ? | 10.2539 |       - |       - |  173.83 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **2,018.20 μs** |     **16.001 μs** |     **8.369 μs** |     **?** |       **?** | **50.7813** |       **-** |       **-** |  **868.51 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  2,070.28 μs |     34.349 μs |     1.883 μs |     ? |       ? | 50.7813 |       - |       - |  868.48 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **38.17 μs** |      **0.066 μs** |     **0.044 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |   **17.39 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     63.80 μs |      6.563 μs |     4.341 μs |  1.67 |    0.11 |  1.0986 |       - |       - |   19.52 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    416.63 μs |      4.385 μs |     2.901 μs | 10.91 |    0.07 | 10.2539 |       - |       - |  171.71 KB |        9.87 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     37.63 μs |      1.394 μs |     0.076 μs |  1.00 |    0.00 |  1.0376 |       - |       - |   17.34 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     68.32 μs |    151.326 μs |     8.295 μs |  1.82 |    0.19 |  1.0986 |       - |       - |   19.26 KB |        1.11 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    407.73 μs |     92.572 μs |     5.074 μs | 10.84 |    0.12 | 10.2539 |       - |       - |   171.7 KB |        9.90 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **4,873.97 μs** |  **2,268.539 μs** | **1,500.498 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **647.21 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,191.24 μs |  8,178.844 μs |   448.310 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  367.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **4,780.73 μs** |  **2,219.760 μs** | **1,468.234 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **655.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,175.58 μs |  8,083.861 μs |   443.104 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  375.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **15,555.32 μs** |  **9,424.092 μs** | **6,233.453 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1609.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  8,363.84 μs | 30,192.903 μs | 1,654.975 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  909.09 KB |           ? |
