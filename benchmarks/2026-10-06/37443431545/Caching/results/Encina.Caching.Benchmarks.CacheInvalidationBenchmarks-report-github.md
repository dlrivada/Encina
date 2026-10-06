```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |   **281.77 μs** |      **2.959 μs** |     **1.957 μs** |     **?** |       **?** |  **1.9531** |       **-** |       **-** |  **171.13 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |   285.20 μs |    159.137 μs |     8.723 μs |     ? |       ? |  1.9531 |       - |       - |  171.15 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        | **1,413.46 μs** |     **13.053 μs** |     **7.767 μs** |     **?** |       **?** |  **9.7656** |       **-** |       **-** |  **855.13 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        | 1,406.48 μs |    136.756 μs |     7.496 μs |     ? |       ? |  9.7656 |       - |       - |  855.11 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |    **26.14 μs** |      **0.161 μs** |     **0.096 μs** |  **1.00** |    **0.00** |  **0.1831** |       **-** |       **-** |   **17.12 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |    41.23 μs |      3.610 μs |     2.388 μs |  1.58 |    0.09 |  0.1831 |       - |       - |    19.1 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |   283.57 μs |      0.853 μs |     0.564 μs | 10.85 |    0.04 |  1.9531 |       - |       - |  169.04 KB |        9.88 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |    26.56 μs |      0.657 μs |     0.036 μs |  1.00 |    0.00 |  0.1831 |       - |       - |   16.92 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |    35.15 μs |     60.437 μs |     3.313 μs |  1.32 |    0.11 |  0.1831 |       - |       - |   18.85 KB |        1.11 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |   285.28 μs |      3.867 μs |     0.212 μs | 10.74 |    0.01 |  1.9531 |       - |       - |  169.04 KB |        9.99 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        | **3,808.63 μs** |  **1,643.250 μs** | **1,086.908 μs** |     **?** |       **?** | **14.6484** | **14.6484** | **14.6484** |  **646.92 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        | 2,579.35 μs |  6,372.001 μs |   349.271 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  366.92 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       | **7,473.36 μs** |  **3,194.867 μs** | **2,113.207 μs** |     **?** |       **?** | **14.6484** | **14.6484** | **14.6484** | **1274.88 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       | 4,798.71 μs | 13,063.029 μs |   716.029 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  714.89 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **9,669.83 μs** |  **4,280.303 μs** | **2,831.155 μs** |     **?** |       **?** | **13.6719** | **13.6719** | **13.6719** | **1608.87 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 6,257.92 μs | 15,438.454 μs |   846.234 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  937.99 KB |           ? |
