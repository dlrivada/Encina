```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **180.50 μs** |      **1.446 μs** |     **0.756 μs** |     **?** |       **?** | **10.2539** |  **0.2441** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    178.92 μs |      3.793 μs |     0.208 μs |     ? |       ? | 10.2539 |  0.2441 |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |    **915.72 μs** |     **37.336 μs** |    **24.696 μs** |     **?** |       **?** | **51.7578** |  **2.9297** |       **-** |  **855.13 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |    922.53 μs |    420.814 μs |    23.066 μs |     ? |       ? | 51.7578 |  2.9297 |       - |  855.13 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **17.88 μs** |      **0.225 μs** |     **0.134 μs** |  **1.00** |    **0.01** |  **1.0071** |  **0.0305** |       **-** |   **16.92 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     26.53 μs |      1.011 μs |     0.601 μs |  1.48 |    0.03 |  1.1597 |  0.0305 |       - |   19.41 KB |        1.15 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    188.13 μs |      8.668 μs |     5.733 μs | 10.52 |    0.31 | 10.2539 |  0.2441 |       - |  169.04 KB |        9.99 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     17.94 μs |      0.878 μs |     0.048 μs |  1.00 |    0.00 |  1.0376 |  0.0305 |       - |   17.02 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     26.02 μs |     16.469 μs |     0.903 μs |  1.45 |    0.04 |  1.1597 |  0.0305 |       - |   19.34 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    180.56 μs |     34.210 μs |     1.875 μs | 10.07 |    0.09 | 10.2539 |  0.2441 |       - |  169.05 KB |        9.93 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **6,532.12 μs** |  **3,403.953 μs** | **2,251.504 μs** |     **?** |       **?** | **17.5781** | **16.1133** | **16.1133** | **1262.91 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,388.83 μs |  7,879.132 μs |   431.882 μs |     ? |       ? | 17.0898 | 15.6250 | 15.6250 |  702.91 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **6,287.53 μs** |  **3,683.480 μs** | **2,436.394 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1266.82 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,921.13 μs | 16,690.582 μs |   914.867 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  706.83 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **12,883.87 μs** | **12,032.387 μs** | **7,958.679 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1588.73 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  5,460.64 μs | 20,922.318 μs | 1,146.823 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  888.72 KB |           ? |
