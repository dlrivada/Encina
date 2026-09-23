```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **377.54 μs** |      **2.498 μs** |     **1.652 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.13 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    379.75 μs |      6.844 μs |     0.375 μs |     ? |       ? | 10.2539 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,930.78 μs** |      **6.647 μs** |     **4.396 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.14 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,922.03 μs |    116.769 μs |     6.400 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.04 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **34.82 μs** |      **0.219 μs** |     **0.145 μs** |  **1.00** |    **0.01** |  **1.0376** |       **-** |       **-** |   **17.34 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     60.92 μs |      8.074 μs |     5.341 μs |  1.75 |    0.15 |  1.0986 |       - |       - |   19.38 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    374.27 μs |      2.343 μs |     1.549 μs | 10.75 |    0.06 | 10.2539 |       - |       - |  169.06 KB |        9.75 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     35.50 μs |      0.636 μs |     0.035 μs |  1.00 |    0.00 |  1.0376 |       - |       - |   17.04 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     55.24 μs |     65.120 μs |     3.569 μs |  1.56 |    0.09 |  1.1597 |       - |       - |   18.96 KB |        1.11 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    384.52 μs |     17.619 μs |     0.966 μs | 10.83 |    0.03 | 10.2539 |       - |       - |  169.03 KB |        9.92 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **6,509.31 μs** |  **3,986.215 μs** | **2,636.634 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.96 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,441.54 μs |  9,825.502 μs |   538.569 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.89 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,980.56 μs** |  **4,121.329 μs** | **2,726.004 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.86 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,612.55 μs | 10,547.315 μs |   578.134 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.83 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **25,967.78 μs** | **15,081.797 μs** | **9,975.675 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.86 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 13,727.76 μs | 41,445.489 μs | 2,271.767 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.88 KB |           ? |
