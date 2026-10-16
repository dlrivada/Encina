```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **340.66 μs** |      **2.464 μs** |      **1.630 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.14 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    357.58 μs |    287.858 μs |     15.778 μs |     ? |       ? | 10.2539 |       - |       - |  171.17 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,745.16 μs** |     **12.346 μs** |      **7.347 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.12 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,726.84 μs |    163.016 μs |      8.935 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.21 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **32.93 μs** |      **0.308 μs** |      **0.161 μs** |  **1.00** |    **0.01** |  **0.9766** |       **-** |       **-** |   **16.75 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     53.35 μs |      2.743 μs |      1.632 μs |  1.62 |    0.05 |  1.1597 |       - |       - |   18.96 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    342.44 μs |      2.077 μs |      1.236 μs | 10.40 |    0.06 | 10.2539 |       - |       - |  169.04 KB |       10.09 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     32.20 μs |      3.577 μs |      0.196 μs |  1.00 |    0.01 |  0.9766 |       - |       - |   16.93 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     50.89 μs |      7.896 μs |      0.433 μs |  1.58 |    0.01 |  1.1597 |       - |       - |   19.23 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    337.08 μs |     29.261 μs |      1.604 μs | 10.47 |    0.07 | 10.2539 |       - |       - |  169.04 KB |        9.98 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **6,011.41 μs** |  **3,454.138 μs** |  **2,284.699 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.91 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,589.35 μs |  9,410.329 μs |    515.812 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.93 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,640.80 μs** |  **3,181.864 μs** |  **2,104.606 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.84 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,738.40 μs | 11,118.358 μs |    609.435 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.87 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **25,838.10 μs** | **19,330.214 μs** | **12,785.739 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.84 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 16,438.21 μs | 76,828.625 μs |  4,211.236 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.85 KB |           ? |
