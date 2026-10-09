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
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |   **178.32 μs** |      **2.209 μs** |     **1.461 μs** |     **?** |       **?** | **10.2539** |  **0.2441** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |   176.17 μs |     18.002 μs |     0.987 μs |     ? |       ? | 10.2539 |  0.2441 |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |   **913.59 μs** |     **34.199 μs** |    **22.621 μs** |     **?** |       **?** | **51.7578** |  **2.9297** |       **-** |  **855.13 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |   947.35 μs |    362.579 μs |    19.874 μs |     ? |       ? | 51.7578 |  2.9297 |       - |  855.11 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |    **16.64 μs** |      **0.209 μs** |     **0.109 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |   **17.01 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |    26.09 μs |      2.046 μs |     1.353 μs |  1.57 |    0.08 |  1.1597 |  0.0305 |       - |    19.4 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |   180.67 μs |      5.094 μs |     3.369 μs | 10.86 |    0.20 | 10.2539 |  0.2441 |       - |  169.05 KB |        9.94 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |    17.61 μs |      9.517 μs |     0.522 μs |  1.00 |    0.04 |  1.0376 |  0.0305 |       - |   17.12 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |    28.01 μs |      5.109 μs |     0.280 μs |  1.59 |    0.04 |  1.1597 |  0.0305 |       - |   19.13 KB |        1.12 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |   184.40 μs |      4.693 μs |     0.257 μs | 10.48 |    0.26 | 10.2539 |  0.2441 |       - |  169.04 KB |        9.88 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        | **6,621.79 μs** |  **3,626.042 μs** | **2,398.402 μs** |     **?** |       **?** | **17.5781** | **16.1133** | **16.1133** | **1262.91 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        | 3,773.64 μs | 16,669.679 μs |   913.721 μs |     ? |       ? | 17.0898 | 15.6250 | 15.6250 |  702.92 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       | **7,071.17 μs** |  **4,932.839 μs** | **3,262.767 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1266.81 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       | 4,176.52 μs | 18,792.649 μs | 1,030.089 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  706.81 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **9,418.77 μs** |  **4,925.028 μs** | **3,257.601 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1588.75 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 4,862.20 μs | 15,308.032 μs |   839.085 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  888.74 KB |           ? |
