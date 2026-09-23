```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **341.17 μs** |      **1.591 μs** |     **1.052 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    340.66 μs |      5.850 μs |     0.321 μs |     ? |       ? | 10.2539 |       - |       - |  171.14 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,695.34 μs** |      **6.885 μs** |     **4.554 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.13 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,705.19 μs |     73.566 μs |     4.032 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.14 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **32.18 μs** |      **0.264 μs** |     **0.175 μs** |  **1.00** |    **0.01** |  **1.0376** |       **-** |       **-** |   **16.96 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     52.81 μs |      2.947 μs |     1.949 μs |  1.64 |    0.06 |  1.1597 |       - |       - |   19.05 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    338.72 μs |      3.006 μs |     1.988 μs | 10.53 |    0.08 | 10.2539 |       - |       - |  169.03 KB |        9.97 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     32.46 μs |      3.020 μs |     0.166 μs |  1.00 |    0.01 |  0.9766 |       - |       - |    16.8 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     49.55 μs |     12.927 μs |     0.709 μs |  1.53 |    0.02 |  1.1597 |       - |       - |   19.41 KB |        1.16 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    339.98 μs |     55.680 μs |     3.052 μs | 10.47 |    0.09 | 10.2539 |       - |       - |  169.03 KB |       10.06 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,286.48 μs** |  **2,421.063 μs** | **1,601.383 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.91 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,425.65 μs |  9,310.765 μs |   510.354 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.92 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,366.14 μs** |  **2,439.082 μs** | **1,613.301 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.88 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,536.57 μs | 10,402.396 μs |   570.190 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  387.38 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **17,309.03 μs** | **10,279.803 μs** | **6,799.453 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.83 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  9,388.07 μs | 32,202.784 μs | 1,765.143 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.82 KB |           ? |
