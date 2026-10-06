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
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **375.34 μs** |      **0.824 μs** |     **0.490 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    387.62 μs |     24.540 μs |     1.345 μs |     ? |       ? | 10.2539 |       - |       - |  171.15 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,930.64 μs** |     **10.041 μs** |     **6.641 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.25 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,911.08 μs |    115.964 μs |     6.356 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.19 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **35.77 μs** |      **0.056 μs** |     **0.033 μs** |  **1.00** |    **0.00** |  **0.9766** |       **-** |       **-** |   **16.66 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     58.51 μs |      5.419 μs |     3.225 μs |  1.64 |    0.09 |  1.1597 |       - |       - |   19.31 KB |        1.16 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    383.15 μs |      0.918 μs |     0.480 μs | 10.71 |    0.02 | 10.2539 |       - |       - |  169.05 KB |       10.14 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     36.32 μs |      1.526 μs |     0.084 μs |  1.00 |    0.00 |  0.9766 |       - |       - |   16.72 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     70.56 μs |     42.025 μs |     2.304 μs |  1.94 |    0.06 |  1.0986 |       - |       - |   19.27 KB |        1.15 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    387.85 μs |      8.067 μs |     0.442 μs | 10.68 |    0.02 | 10.2539 |       - |       - |  169.04 KB |       10.11 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,047.28 μs** |  **2,344.388 μs** | **1,550.668 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.94 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  2,983.26 μs |  7,090.020 μs |   388.628 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.93 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,190.09 μs** |  **2,463.153 μs** | **1,629.223 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.88 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,310.60 μs |  9,055.603 μs |   496.368 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.89 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **21,750.74 μs** | **13,697.175 μs** | **9,059.833 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.87 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  9,956.46 μs | 42,655.245 μs | 2,338.078 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.82 KB |           ? |
