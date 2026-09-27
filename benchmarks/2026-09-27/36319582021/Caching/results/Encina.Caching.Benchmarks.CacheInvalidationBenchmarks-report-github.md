```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **381.12 μs** |      **1.698 μs** |     **1.123 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.14 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    384.33 μs |     41.087 μs |     2.252 μs |     ? |       ? | 10.2539 |       - |       - |  171.14 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,877.90 μs** |      **9.648 μs** |     **5.742 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.19 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,896.10 μs |     44.629 μs |     2.446 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.14 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **35.34 μs** |      **0.085 μs** |     **0.056 μs** |  **1.00** |    **0.00** |  **0.9766** |       **-** |       **-** |   **16.84 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     58.38 μs |      2.689 μs |     1.406 μs |  1.65 |    0.04 |  1.0986 |       - |       - |   18.93 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    377.50 μs |      2.074 μs |     1.234 μs | 10.68 |    0.04 | 10.2539 |       - |       - |  169.06 KB |       10.04 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     36.58 μs |     13.099 μs |     0.718 μs |  1.00 |    0.02 |  0.9766 |       - |       - |   16.87 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     63.19 μs |     97.822 μs |     5.362 μs |  1.73 |    0.13 |  1.1597 |       - |       - |   19.09 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    382.36 μs |      8.053 μs |     0.441 μs | 10.46 |    0.18 | 10.2539 |       - |       - |  169.02 KB |       10.02 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,386.12 μs** |  **2,627.126 μs** | **1,737.681 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.92 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,242.90 μs |  7,985.200 μs |   437.696 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |   366.9 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,139.15 μs** |  **2,519.969 μs** | **1,666.804 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.86 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,058.38 μs |  6,892.212 μs |   377.785 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.86 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **15,889.83 μs** | **10,573.830 μs** | **6,993.934 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.87 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  8,914.34 μs | 35,432.140 μs | 1,942.155 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  938.51 KB |           ? |
