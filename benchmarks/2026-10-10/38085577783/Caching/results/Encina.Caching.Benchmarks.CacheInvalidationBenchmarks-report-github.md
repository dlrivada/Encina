```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **374.55 μs** |      **1.037 μs** |      **0.686 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    386.62 μs |     16.689 μs |      0.915 μs |     ? |       ? | 10.2539 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,889.39 μs** |      **8.311 μs** |      **4.946 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.18 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,909.44 μs |     12.271 μs |      0.673 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.15 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **36.50 μs** |      **0.060 μs** |      **0.039 μs** |  **1.00** |    **0.00** |  **0.9766** |       **-** |       **-** |   **16.88 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     58.75 μs |      3.458 μs |      2.058 μs |  1.61 |    0.05 |  1.0986 |       - |       - |   19.31 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    380.65 μs |      0.570 μs |      0.298 μs | 10.43 |    0.01 | 10.2539 |       - |       - |  169.04 KB |       10.02 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     36.02 μs |      0.671 μs |      0.037 μs |  1.00 |    0.00 |  1.0376 |       - |       - |   17.05 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     56.70 μs |     26.704 μs |      1.464 μs |  1.57 |    0.04 |  1.1597 |       - |       - |   19.31 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    382.11 μs |      9.865 μs |      0.541 μs | 10.61 |    0.02 | 10.2539 |       - |       - |  169.05 KB |        9.91 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **6,280.13 μs** |  **4,186.469 μs** |  **2,769.090 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.96 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,339.05 μs | 11,923.596 μs |    653.573 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.88 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,395.81 μs** |  **3,399.587 μs** |  **2,023.040 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.87 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,639.06 μs | 13,188.811 μs |    722.923 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  387.53 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **26,890.42 μs** | **15,603.638 μs** | **10,320.841 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.85 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 13,784.72 μs | 66,729.302 μs |  3,657.658 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.83 KB |           ? |
