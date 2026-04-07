```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | keyCount | concurrencyLevel | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |--------- |----------------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**        | **?**                |     **35.89 μs** |      **0.295 μs** |     **0.195 μs** |  **1.00** |    **0.01** |  **0.9766** |       **-** |       **-** |   **16.85 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?        | ?                |     57.07 μs |      5.502 μs |     3.639 μs |  1.59 |    0.10 |  1.1597 |       - |       - |    19.3 KB |        1.15 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?        | ?                |    380.54 μs |      2.651 μs |     1.754 μs | 10.60 |    0.07 | 10.2539 |       - |       - |  169.03 KB |       10.03 |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?        | ?                |     34.99 μs |      2.058 μs |     0.113 μs |  1.00 |    0.00 |  0.9766 |       - |       - |   16.87 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?        | ?                |     53.74 μs |     79.176 μs |     4.340 μs |  1.54 |    0.11 |  1.1597 |       - |       - |   18.96 KB |        1.12 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?        | ?                |    383.91 μs |     42.761 μs |     2.344 μs | 10.97 |    0.07 | 10.2539 |       - |       - |  169.04 KB |       10.02 |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **5**        | **?**                |  **5,146.15 μs** |  **2,833.704 μs** | **1,874.320 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.92 KB** |           **?** |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | 5        | ?                |  3,262.20 μs |  8,467.921 μs |   464.155 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.92 KB |           ? |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **?**        | **10**               |    **376.75 μs** |      **1.417 μs** |     **0.843 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.14 KB** |           **?** |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | ?        | 10               |    382.07 μs |     10.360 μs |     0.568 μs |     ? |       ? | 10.2539 |       - |       - |  171.17 KB |           ? |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **10**       | **?**                |  **5,415.81 μs** |  **2,687.931 μs** | **1,777.900 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.87 KB** |           **?** |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | 10       | ?                |  3,117.31 μs |  9,314.246 μs |   510.545 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.85 KB |           ? |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **25**       | **?**                | **16,472.91 μs** |  **8,133.308 μs** | **5,379.680 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.86 KB** |           **?** |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | 25       | ?                |  9,191.82 μs | 31,541.625 μs | 1,728.903 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.81 KB |           ? |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **?**        | **50**               |  **1,897.43 μs** |      **7.612 μs** |     **5.035 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.13 KB** |           **?** |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | ?        | 50               |  1,874.25 μs |     83.994 μs |     4.604 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.17 KB |           ? |
