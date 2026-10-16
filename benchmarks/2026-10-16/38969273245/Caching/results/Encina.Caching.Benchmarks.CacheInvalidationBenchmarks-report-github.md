```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **283.71 μs** |      **4.353 μs** |      **2.590 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **173.82 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    282.58 μs |    106.764 μs |      5.852 μs |     ? |       ? | 10.2539 |       - |       - |  173.81 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,411.64 μs** |     **17.334 μs** |     **10.315 μs** |     **?** |       **?** | **52.7344** |  **1.9531** |       **-** |  **868.42 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,425.83 μs |    315.686 μs |     17.304 μs |     ? |       ? | 52.7344 |  1.9531 |       - |  868.44 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **26.85 μs** |      **0.281 μs** |      **0.147 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |   **17.13 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     42.57 μs |      3.703 μs |      2.449 μs |  1.59 |    0.09 |  1.1597 |       - |       - |   19.49 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    281.83 μs |      2.400 μs |      1.428 μs | 10.50 |    0.07 | 10.2539 |       - |       - |  171.71 KB |       10.03 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     26.86 μs |      2.732 μs |      0.150 μs |  1.00 |    0.01 |  1.0376 |  0.0305 |       - |   17.26 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     44.39 μs |      9.328 μs |      0.511 μs |  1.65 |    0.02 |  1.1597 |       - |       - |   19.57 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    281.08 μs |      3.684 μs |      0.202 μs | 10.46 |    0.05 | 10.2539 |       - |       - |  171.71 KB |        9.95 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **4,819.42 μs** |  **2,451.743 μs** |  **1,621.676 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **647.21 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  2,782.08 μs |  7,423.720 μs |    406.919 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |   367.2 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **9,292.38 μs** |  **5,022.997 μs** |  **3,322.401 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1275.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  5,397.73 μs | 19,041.764 μs |  1,043.743 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  715.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **19,009.87 μs** | **16,211.852 μs** | **10,723.136 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1609.12 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  7,555.98 μs | 25,559.587 μs |  1,401.007 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  909.12 KB |           ? |
