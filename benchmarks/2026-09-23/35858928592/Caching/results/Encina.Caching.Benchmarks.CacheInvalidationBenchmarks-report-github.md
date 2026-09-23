```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **383.48 μs** |      **1.089 μs** |     **0.570 μs** |     **?** |       **?** |  **6.8359** |       **-** |       **-** |  **171.17 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    379.81 μs |     13.730 μs |     0.753 μs |     ? |       ? |  6.8359 |       - |       - |  171.15 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,922.92 μs** |      **3.129 μs** |     **1.636 μs** |     **?** |       **?** | **33.2031** |  **1.9531** |       **-** |  **855.17 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,893.34 μs |     46.288 μs |     2.537 μs |     ? |       ? | 33.2031 |  1.9531 |       - |  855.11 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **37.38 μs** |      **0.081 μs** |     **0.048 μs** |  **1.00** |    **0.00** |  **0.6714** |       **-** |       **-** |   **16.91 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     55.58 μs |      4.538 μs |     2.700 μs |  1.49 |    0.07 |  0.7324 |       - |       - |   19.07 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    379.29 μs |      1.652 μs |     0.983 μs | 10.15 |    0.03 |  6.8359 |       - |       - |  169.04 KB |       10.00 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     37.29 μs |      0.797 μs |     0.044 μs |  1.00 |    0.00 |  0.6714 |       - |       - |   16.85 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     49.84 μs |     36.794 μs |     2.017 μs |  1.34 |    0.05 |  0.7935 |       - |       - |   19.45 KB |        1.15 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    376.88 μs |      1.838 μs |     0.101 μs | 10.11 |    0.01 |  6.8359 |       - |       - |  169.05 KB |       10.03 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,155.00 μs** |  **2,626.468 μs** | **1,737.246 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.94 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,242.01 μs |  8,598.575 μs |   471.317 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  366.91 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,183.71 μs** |  **2,375.229 μs** | **1,571.067 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.88 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,368.62 μs |  7,450.550 μs |   408.390 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.88 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **21,025.81 μs** | **13,930.666 μs** | **9,214.273 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **1608.86 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 12,722.51 μs | 63,706.897 μs | 3,491.990 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  908.82 KB |           ? |
