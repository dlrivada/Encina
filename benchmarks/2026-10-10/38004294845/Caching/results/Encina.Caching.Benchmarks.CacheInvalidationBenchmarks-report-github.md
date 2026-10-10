```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **267.03 μs** |      **2.597 μs** |     **1.717 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.17 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    267.47 μs |     33.833 μs |     1.855 μs |     ? |       ? | 10.2539 |       - |       - |  171.15 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,333.27 μs** |      **7.364 μs** |     **4.871 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.17 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,356.83 μs |    353.833 μs |    19.395 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.23 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **24.98 μs** |      **0.123 μs** |     **0.073 μs** |  **1.00** |    **0.00** |  **1.0376** |  **0.0305** |       **-** |   **17.05 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     40.25 μs |      1.193 μs |     0.789 μs |  1.61 |    0.03 |  1.1597 |       - |       - |   19.16 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    262.37 μs |      1.424 μs |     0.942 μs | 10.50 |    0.05 | 10.2539 |       - |       - |  169.05 KB |        9.92 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     25.52 μs |      2.409 μs |     0.132 μs |  1.00 |    0.01 |  1.0071 |  0.0305 |       - |   16.85 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     40.97 μs |     16.209 μs |     0.888 μs |  1.61 |    0.03 |  1.1597 |       - |       - |   19.28 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    267.28 μs |     89.680 μs |     4.916 μs | 10.47 |    0.17 | 10.2539 |       - |       - |  169.05 KB |       10.03 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **4,531.23 μs** |  **2,482.843 μs** | **1,642.247 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.94 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  2,752.45 μs |  7,617.736 μs |   417.554 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.91 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       | **16,284.85 μs** | **12,580.980 μs** | **8,321.539 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1274.88 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  5,889.67 μs | 23,121.731 μs | 1,267.380 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  714.84 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **20,040.26 μs** | **14,220.008 μs** | **9,405.655 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.89 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  7,748.40 μs | 25,740.807 μs | 1,410.940 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.84 KB |           ? |
