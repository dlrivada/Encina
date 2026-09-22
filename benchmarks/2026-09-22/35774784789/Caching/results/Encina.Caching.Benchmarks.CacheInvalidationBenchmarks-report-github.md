```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **325.29 μs** |      **2.851 μs** |     **1.886 μs** |     **?** |       **?** |  **1.9531** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    322.38 μs |     31.748 μs |     1.740 μs |     ? |       ? |  1.9531 |       - |       - |  171.14 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,628.36 μs** |     **11.264 μs** |     **6.703 μs** |     **?** |       **?** |  **9.7656** |       **-** |       **-** |  **855.13 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,627.49 μs |    142.173 μs |     7.793 μs |     ? |       ? |  9.7656 |       - |       - |  855.21 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **31.28 μs** |      **0.215 μs** |     **0.142 μs** |  **1.00** |    **0.01** |  **0.1831** |       **-** |       **-** |   **16.93 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     43.13 μs |      4.761 μs |     3.149 μs |  1.38 |    0.10 |  0.1831 |       - |       - |   19.63 KB |        1.16 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    322.85 μs |      0.816 μs |     0.427 μs | 10.32 |    0.05 |  1.9531 |       - |       - |  169.05 KB |        9.99 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     29.60 μs |      0.508 μs |     0.028 μs |  1.00 |    0.00 |  0.1831 |       - |       - |   17.26 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     42.40 μs |      8.601 μs |     0.471 μs |  1.43 |    0.01 |  0.1831 |       - |       - |   19.34 KB |        1.12 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    326.68 μs |     39.742 μs |     2.178 μs | 11.03 |    0.06 |  1.9531 |       - |       - |  169.07 KB |        9.80 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **4,380.44 μs** |  **1,904.239 μs** | **1,259.536 μs** |     **?** |       **?** | **14.6484** | **14.6484** | **14.6484** |  **646.92 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  2,960.63 μs |  6,913.212 μs |   378.936 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  366.92 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **4,629.62 μs** |  **2,010.255 μs** | **1,329.659 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.82 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,008.50 μs |  7,338.780 μs |   402.263 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |   374.9 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **13,279.00 μs** |  **7,649.794 μs** | **5,059.865 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **1608.87 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  7,497.32 μs | 20,450.153 μs | 1,120.942 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  908.85 KB |           ? |
