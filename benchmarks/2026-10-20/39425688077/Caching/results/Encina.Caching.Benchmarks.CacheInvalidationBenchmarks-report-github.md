```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **418.24 μs** |      **2.099 μs** |      **1.098 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **173.82 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    413.62 μs |     26.092 μs |      1.430 μs |     ? |       ? | 10.2539 |       - |       - |  173.81 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **2,081.23 μs** |      **9.282 μs** |      **5.524 μs** |     **?** |       **?** | **50.7813** |       **-** |       **-** |   **868.5 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  2,045.65 μs |     45.488 μs |      2.493 μs |     ? |       ? | 50.7813 |       - |       - |  868.41 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **38.27 μs** |      **0.132 μs** |      **0.079 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |   **17.35 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     61.22 μs |      6.337 μs |      4.192 μs |  1.60 |    0.10 |  1.1597 |  0.0610 |       - |    19.5 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    411.55 μs |      2.109 μs |      1.395 μs | 10.75 |    0.04 | 10.2539 |       - |       - |   171.7 KB |        9.89 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     38.28 μs |      6.793 μs |      0.372 μs |  1.00 |    0.01 |  1.0376 |       - |       - |   17.43 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     55.17 μs |     12.398 μs |      0.680 μs |  1.44 |    0.02 |  1.1597 |       - |       - |   19.66 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    414.82 μs |     33.584 μs |      1.841 μs | 10.84 |    0.10 | 10.2539 |       - |       - |  171.71 KB |        9.85 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,768.53 μs** |  **3,510.441 μs** |  **2,321.939 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |   **647.2 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,361.24 μs |  9,873.035 μs |    541.174 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  367.19 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,430.60 μs** |  **2,701.453 μs** |  **1,786.844 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **655.17 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,416.73 μs |  9,310.963 μs |    510.365 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  375.14 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **24,733.39 μs** | **18,285.672 μs** | **12,094.840 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1609.17 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 12,864.27 μs | 49,294.344 μs |  2,701.989 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  909.13 KB |           ? |
