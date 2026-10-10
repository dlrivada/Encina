```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **376.60 μs** |      **1.616 μs** |     **0.962 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.14 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    385.66 μs |    106.740 μs |     5.851 μs |     ? |       ? | 10.2539 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,885.75 μs** |      **6.486 μs** |     **4.290 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.17 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,901.16 μs |    110.629 μs |     6.064 μs |     ? |       ? | 50.7813 |  1.9531 |       - |   855.1 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **35.54 μs** |      **0.120 μs** |     **0.072 μs** |  **1.00** |    **0.00** |  **0.9766** |       **-** |       **-** |   **16.82 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     61.62 μs |      8.647 μs |     5.719 μs |  1.73 |    0.15 |  1.0986 |       - |       - |   18.95 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    387.54 μs |      0.916 μs |     0.545 μs | 10.90 |    0.03 | 10.2539 |       - |       - |  169.06 KB |       10.05 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     35.24 μs |      1.164 μs |     0.064 μs |  1.00 |    0.00 |  1.0376 |       - |       - |   17.28 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     60.66 μs |     36.960 μs |     2.026 μs |  1.72 |    0.05 |  1.0986 |       - |       - |   19.26 KB |        1.11 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    385.56 μs |     22.439 μs |     1.230 μs | 10.94 |    0.03 | 10.2539 |       - |       - |  169.07 KB |        9.78 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **8,264.84 μs** |  **5,396.622 μs** | **3,569.531 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.96 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  4,077.13 μs | 16,290.443 μs |   892.934 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.92 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **7,781.68 μs** |  **5,769.470 μs** | **3,816.147 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.86 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,896.74 μs | 17,832.617 μs |   977.466 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.89 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **28,045.28 μs** | **14,252.625 μs** | **9,427.229 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.89 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 14,563.13 μs | 88,976.533 μs | 4,877.104 μs |     ? |       ? | 19.5313 | 17.5781 | 17.5781 |  937.65 KB |           ? |
