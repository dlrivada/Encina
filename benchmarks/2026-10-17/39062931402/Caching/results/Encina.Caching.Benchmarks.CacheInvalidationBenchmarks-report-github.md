```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |   **185.70 μs** |      **2.772 μs** |     **1.450 μs** |     **?** |       **?** | **10.4980** |  **0.2441** |       **-** |  **173.82 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |   186.41 μs |     26.263 μs |     1.440 μs |     ? |       ? | 10.4980 |  0.2441 |       - |  173.83 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |   **949.27 μs** |     **23.925 μs** |    **15.825 μs** |     **?** |       **?** | **52.7344** |  **2.9297** |       **-** |  **868.48 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |   952.26 μs |    407.355 μs |    22.328 μs |     ? |       ? | 52.7344 |  2.9297 |       - |  868.47 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |    **17.65 μs** |      **0.417 μs** |     **0.248 μs** |  **1.00** |    **0.02** |  **1.0376** |  **0.0305** |       **-** |   **17.34 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |    28.91 μs |      1.310 μs |     0.779 μs |  1.64 |    0.05 |  1.1597 |       - |       - |   19.44 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |   184.26 μs |      4.933 μs |     3.263 μs | 10.44 |    0.23 | 10.4980 |  0.2441 |       - |   171.7 KB |        9.90 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |    18.04 μs |      4.663 μs |     0.256 μs |  1.00 |    0.02 |  1.0376 |  0.0305 |       - |   17.19 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |    29.63 μs |     43.098 μs |     2.362 μs |  1.64 |    0.12 |  1.1597 |       - |       - |    19.5 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |   188.40 μs |     39.015 μs |     2.139 μs | 10.44 |    0.16 | 10.4980 |  0.2441 |       - |  171.71 KB |        9.99 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        | **2,873.35 μs** |  **1,311.993 μs** |   **867.802 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **647.21 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        | 1,851.95 μs |  4,356.984 μs |   238.821 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  367.21 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       | **6,766.74 μs** |  **4,099.628 μs** | **2,711.650 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1267.14 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       | 3,700.88 μs | 11,424.683 μs |   626.225 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  715.16 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **9,061.53 μs** |  **5,265.937 μs** | **3,483.091 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1609.15 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 5,054.95 μs | 15,587.139 μs |   854.384 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  909.12 KB |           ? |
