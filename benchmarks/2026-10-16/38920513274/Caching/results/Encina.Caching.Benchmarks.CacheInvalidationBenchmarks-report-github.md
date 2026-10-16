```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **385.93 μs** |      **0.802 μs** |     **0.530 μs** |     **?** |       **?** |  **6.8359** |       **-** |       **-** |  **171.17 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    379.78 μs |     77.279 μs |     4.236 μs |     ? |       ? |  6.8359 |       - |       - |  171.15 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,908.44 μs** |      **4.853 μs** |     **2.888 μs** |     **?** |       **?** | **33.2031** |  **1.9531** |       **-** |  **855.12 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,874.80 μs |    145.217 μs |     7.960 μs |     ? |       ? | 33.2031 |  1.9531 |       - |   855.2 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **36.88 μs** |      **0.464 μs** |     **0.307 μs** |  **1.00** |    **0.01** |  **0.6714** |       **-** |       **-** |   **16.91 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     55.49 μs |      8.659 μs |     5.727 μs |  1.50 |    0.15 |  0.7324 |  0.2441 |       - |   19.32 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    373.30 μs |      0.848 μs |     0.505 μs | 10.12 |    0.08 |  6.8359 |       - |       - |  169.04 KB |        9.99 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     36.59 μs |      1.422 μs |     0.078 μs |  1.00 |    0.00 |  0.6714 |       - |       - |   16.81 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     48.69 μs |     43.467 μs |     2.383 μs |  1.33 |    0.06 |  0.7324 |       - |       - |   19.27 KB |        1.15 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    371.14 μs |     11.493 μs |     0.630 μs | 10.14 |    0.02 |  6.8359 |       - |       - |  169.05 KB |       10.06 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **8,798.09 μs** |  **5,924.750 μs** | **3,918.856 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.93 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  4,744.34 μs | 13,452.441 μs |   737.374 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.93 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **9,393.78 μs** |  **4,458.534 μs** | **2,949.044 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.91 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  4,853.82 μs | 31,104.753 μs | 1,704.956 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.91 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **26,974.17 μs** | **12,759.660 μs** | **8,439.725 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **1608.89 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 15,544.26 μs | 46,646.413 μs | 2,556.847 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  938.17 KB |           ? |
