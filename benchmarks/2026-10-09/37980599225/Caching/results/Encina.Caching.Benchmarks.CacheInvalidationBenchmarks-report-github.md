```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |   **178.96 μs** |      **3.187 μs** |     **1.667 μs** |     **?** |       **?** | **10.2539** |  **0.2441** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |   179.54 μs |     80.984 μs |     4.439 μs |     ? |       ? | 10.2539 |  0.2441 |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |   **929.71 μs** |     **31.147 μs** |    **20.602 μs** |     **?** |       **?** | **51.7578** |  **2.9297** |       **-** |  **855.16 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |   894.06 μs |     48.929 μs |     2.682 μs |     ? |       ? | 51.7578 |  2.9297 |       - |  855.13 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |    **16.73 μs** |      **0.253 μs** |     **0.167 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |   **17.19 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |    26.01 μs |      2.156 μs |     1.426 μs |  1.55 |    0.08 |  1.1597 |  0.0305 |       - |   19.23 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |   182.67 μs |      3.364 μs |     2.002 μs | 10.92 |    0.15 | 10.2539 |  0.2441 |       - |  169.04 KB |        9.84 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |    17.49 μs |      0.782 μs |     0.043 μs |  1.00 |    0.00 |  1.0071 |  0.0305 |       - |   16.86 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |    27.80 μs |      6.252 μs |     0.343 μs |  1.59 |    0.02 |  1.1597 |  0.0305 |       - |   19.26 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |   182.11 μs |     28.467 μs |     1.560 μs | 10.41 |    0.08 | 10.2539 |  0.2441 |       - |  169.05 KB |       10.03 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        | **7,317.58 μs** |  **3,945.102 μs** | **2,347.667 μs** |     **?** |       **?** | **17.5781** | **16.1133** | **16.1133** |  **1262.9 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        | 4,008.45 μs | 14,269.975 μs |   782.185 μs |     ? |       ? | 17.0898 | 15.6250 | 15.6250 |  702.93 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       | **7,548.15 μs** |  **4,726.701 μs** | **3,126.420 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1266.81 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       | 4,625.02 μs | 21,631.489 μs | 1,185.695 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  706.85 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **9,164.26 μs** |  **6,412.488 μs** | **4,241.464 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1588.75 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 5,086.57 μs | 16,074.384 μs |   881.091 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  888.74 KB |           ? |
