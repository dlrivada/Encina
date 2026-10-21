```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **409.65 μs** |      **1.691 μs** |      **1.119 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **173.83 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    411.90 μs |     14.796 μs |      0.811 μs |     ? |       ? | 10.2539 |       - |       - |  173.84 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **2,083.85 μs** |      **9.047 μs** |      **4.732 μs** |     **?** |       **?** | **50.7813** |       **-** |       **-** |  **868.39 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  2,116.02 μs |    600.362 μs |     32.908 μs |     ? |       ? | 50.7813 |       - |       - |  868.41 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **39.21 μs** |      **0.215 μs** |      **0.142 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |   **17.14 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     62.67 μs |      7.790 μs |      5.153 μs |  1.60 |    0.13 |  1.1597 |       - |       - |   19.52 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    404.52 μs |      2.297 μs |      1.367 μs | 10.32 |    0.05 | 10.2539 |       - |       - |  171.72 KB |       10.02 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     39.32 μs |      9.299 μs |      0.510 μs |  1.00 |    0.02 |  1.0376 |       - |       - |   17.21 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     68.78 μs |    126.601 μs |      6.939 μs |  1.75 |    0.15 |  1.0986 |       - |       - |   19.48 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    413.91 μs |     60.621 μs |      3.323 μs | 10.53 |    0.14 | 10.2539 |       - |       - |   171.7 KB |        9.98 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **6,961.14 μs** |  **5,010.540 μs** |  **3,314.162 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **647.24 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,682.50 μs | 10,216.041 μs |    559.976 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  367.17 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **7,574.23 μs** |  **4,677.994 μs** |  **3,094.203 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **655.14 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,636.22 μs | 13,087.126 μs |    717.350 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  375.17 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **23,076.44 μs** | **15,939.260 μs** | **10,542.833 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1609.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 16,327.65 μs | 69,614.315 μs |  3,815.796 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  938.73 KB |           ? |
