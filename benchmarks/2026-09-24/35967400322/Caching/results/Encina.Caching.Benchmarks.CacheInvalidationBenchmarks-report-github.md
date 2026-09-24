```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.82GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **378.78 μs** |      **1.275 μs** |      **0.843 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    384.06 μs |     63.722 μs |      3.493 μs |     ? |       ? | 10.2539 |       - |       - |  171.15 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,893.44 μs** |     **24.978 μs** |     **16.521 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,874.44 μs |     30.670 μs |      1.681 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.18 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **35.63 μs** |      **0.160 μs** |      **0.106 μs** |  **1.00** |    **0.00** |  **0.9766** |       **-** |       **-** |   **16.88 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     55.90 μs |      2.060 μs |      1.226 μs |  1.57 |    0.03 |  1.0986 |       - |       - |   18.93 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    379.27 μs |      0.992 μs |      0.519 μs | 10.64 |    0.03 | 10.2539 |       - |       - |  169.06 KB |       10.01 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     34.95 μs |      0.345 μs |      0.019 μs |  1.00 |    0.00 |  1.0376 |       - |       - |   17.09 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     56.12 μs |     65.351 μs |      3.582 μs |  1.61 |    0.09 |  1.1597 |       - |       - |   19.24 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    382.00 μs |     24.859 μs |      1.363 μs | 10.93 |    0.03 | 10.2539 |       - |       - |  169.03 KB |        9.89 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **6,279.46 μs** |  **3,410.611 μs** |  **2,255.908 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.95 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,344.67 μs |  9,574.540 μs |    524.813 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.91 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **4,715.50 μs** |  **2,070.606 μs** |  **1,369.577 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.89 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,587.35 μs | 13,319.350 μs |    730.078 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.86 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **27,347.69 μs** | **15,825.893 μs** | **10,467.849 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.86 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 16,761.82 μs | 54,295.918 μs |  2,976.142 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  938.49 KB |           ? |
