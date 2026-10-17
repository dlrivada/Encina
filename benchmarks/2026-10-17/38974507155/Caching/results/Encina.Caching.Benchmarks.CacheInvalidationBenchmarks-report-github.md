```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **330.90 μs** |      **3.833 μs** |     **2.005 μs** |     **?** |       **?** |  **1.9531** |       **-** |       **-** | **173.81 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    337.97 μs |     71.764 μs |     3.934 μs |     ? |       ? |  1.9531 |       - |       - | 173.82 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,668.97 μs** |     **32.062 μs** |    **19.079 μs** |     **?** |       **?** |  **9.7656** |       **-** |       **-** | **868.51 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,661.67 μs |    308.744 μs |    16.923 μs |     ? |       ? |  9.7656 |       - |       - | 868.48 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **31.88 μs** |      **0.389 μs** |     **0.257 μs** |  **1.00** |    **0.01** |  **0.1831** |       **-** |       **-** |  **17.19 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     53.78 μs |      8.371 μs |     4.981 μs |  1.69 |    0.15 |  0.1221 |       - |       - |  19.48 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    336.38 μs |      5.464 μs |     3.252 μs | 10.55 |    0.13 |  1.9531 |       - |       - |  171.7 KB |        9.99 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     31.73 μs |      1.707 μs |     0.094 μs |  1.00 |    0.00 |  0.1831 |       - |       - |  17.32 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     57.69 μs |    110.186 μs |     6.040 μs |  1.82 |    0.16 |  0.1831 |       - |       - |  19.62 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    333.17 μs |     33.957 μs |     1.861 μs | 10.50 |    0.06 |  1.9531 |       - |       - | 171.69 KB |        9.91 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,344.74 μs** |  **2,270.378 μs** | **1,501.715 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **647.22 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,565.30 μs |  8,571.130 μs |   469.813 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 | 367.19 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,268.30 μs** |  **2,049.874 μs** | **1,355.864 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **655.17 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,554.87 μs |  5,955.186 μs |   326.424 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 | 375.13 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **13,515.22 μs** |  **6,424.074 μs** | **4,249.127 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **1609.1 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |           |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  9,316.43 μs | 24,814.271 μs | 1,360.154 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 | 909.11 KB |           ? |
