```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **224.54 μs** |      **0.749 μs** |     **0.446 μs** |     **?** |       **?** | **10.4980** |  **0.2441** |       **-** |   **173.8 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    226.96 μs |      8.644 μs |     0.474 μs |     ? |       ? | 10.4980 |  0.2441 |       - |  173.82 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,123.61 μs** |      **4.544 μs** |     **3.006 μs** |     **?** |       **?** | **52.7344** |  **1.9531** |       **-** |  **868.49 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,182.38 μs |  1,013.380 μs |    55.547 μs |     ? |       ? | 52.7344 |  1.9531 |       - |  868.49 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **21.76 μs** |      **0.290 μs** |     **0.172 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |   **17.19 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     35.27 μs |      3.218 μs |     2.128 μs |  1.62 |    0.09 |  1.1597 |       - |       - |   19.62 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    226.85 μs |      1.643 μs |     1.087 μs | 10.43 |    0.09 | 10.4980 |  0.2441 |       - |   171.7 KB |        9.99 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     21.48 μs |      1.191 μs |     0.065 μs |  1.00 |    0.00 |  1.0376 |  0.0305 |       - |   17.13 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     36.18 μs |     13.519 μs |     0.741 μs |  1.68 |    0.03 |  1.1597 |       - |       - |   19.23 KB |        1.12 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    233.12 μs |     40.397 μs |     2.214 μs | 10.85 |    0.09 | 10.4980 |  0.2441 |       - |  171.69 KB |       10.02 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **3,448.24 μs** |  **1,520.503 μs** | **1,005.719 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **647.21 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  2,277.30 μs |  6,176.032 μs |   338.529 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  373.36 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **7,773.70 μs** |  **4,739.786 μs** | **3,135.075 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1275.18 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  4,512.91 μs | 16,589.827 μs |   909.344 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  715.14 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **10,430.03 μs** |  **6,271.398 μs** | **4,148.142 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1609.14 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  6,108.34 μs | 20,283.190 μs | 1,111.790 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |   938.6 KB |           ? |
