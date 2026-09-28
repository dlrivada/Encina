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
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **377.77 μs** |      **1.277 μs** |     **0.668 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.14 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    374.45 μs |     12.827 μs |     0.703 μs |     ? |       ? | 10.2539 |       - |       - |  171.14 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,889.47 μs** |      **6.097 μs** |     **3.628 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.14 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,874.21 μs |     85.692 μs |     4.697 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.21 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **35.94 μs** |      **0.167 μs** |     **0.111 μs** |  **1.00** |    **0.00** |  **0.9766** |       **-** |       **-** |   **16.78 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     57.14 μs |      5.084 μs |     3.363 μs |  1.59 |    0.09 |  1.1597 |       - |       - |   19.19 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    371.49 μs |      0.709 μs |     0.371 μs | 10.34 |    0.03 | 10.2539 |       - |       - |  169.05 KB |       10.07 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     33.37 μs |      1.380 μs |     0.076 μs |  1.00 |    0.00 |  1.0376 |       - |       - |   17.38 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     62.58 μs |     85.962 μs |     4.712 μs |  1.88 |    0.12 |  1.1597 |       - |       - |   19.32 KB |        1.11 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    373.54 μs |     32.845 μs |     1.800 μs | 11.19 |    0.05 | 10.2539 |       - |       - |  169.04 KB |        9.73 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,078.68 μs** |  **2,620.559 μs** | **1,733.337 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.91 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,193.82 μs |  8,741.353 μs |   479.143 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.92 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **4,871.94 μs** |  **2,302.580 μs** | **1,523.014 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.86 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,161.62 μs |  7,328.086 μs |   401.677 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.82 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **13,834.28 μs** |  **8,218.671 μs** | **5,436.142 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.87 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  8,419.38 μs | 26,794.623 μs | 1,468.704 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.82 KB |           ? |
