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
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |   **178.45 μs** |      **3.239 μs** |     **2.142 μs** |     **?** |       **?** | **10.2539** |  **0.2441** |       **-** |  **171.17 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |   182.66 μs |     59.001 μs |     3.234 μs |     ? |       ? | 10.2539 |  0.2441 |       - |  171.15 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |   **870.74 μs** |     **10.190 μs** |     **6.064 μs** |     **?** |       **?** | **51.7578** |  **2.9297** |       **-** |  **855.12 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |   880.96 μs |     36.357 μs |     1.993 μs |     ? |       ? | 51.7578 |  2.9297 |       - |  855.15 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |    **16.67 μs** |      **0.172 μs** |     **0.114 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |   **16.98 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |    24.87 μs |      1.436 μs |     0.950 μs |  1.49 |    0.06 |  1.1597 |  0.0305 |       - |   19.22 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |   175.99 μs |      1.719 μs |     1.023 μs | 10.56 |    0.09 | 10.2539 |  0.2441 |       - |  169.05 KB |        9.96 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |    17.34 μs |      0.824 μs |     0.045 μs |  1.00 |    0.00 |  1.0071 |  0.0305 |       - |   16.94 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |    25.79 μs |     12.222 μs |     0.670 μs |  1.49 |    0.03 |  1.1597 |  0.0305 |       - |   19.31 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |   181.98 μs |     29.524 μs |     1.618 μs | 10.50 |    0.08 | 10.2539 |  0.2441 |       - |  169.05 KB |        9.98 |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        | **6,963.80 μs** |  **4,696.584 μs** | **3,106.499 μs** |     **?** |       **?** | **17.5781** | **16.1133** | **16.1133** | **1262.91 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        | 2,066.37 μs |  5,264.116 μs |   288.544 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  362.93 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       | **6,138.80 μs** |  **3,463.563 μs** | **2,290.933 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1266.82 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       | 3,629.62 μs | 11,785.258 μs |   645.990 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |   706.8 KB |           ? |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **9,473.54 μs** |  **5,607.201 μs** | **3,708.816 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1588.75 KB** |           **?** |
|                                 |            |                |             |                  |          |             |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 5,121.51 μs | 16,407.905 μs |   899.373 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  888.71 KB |           ? |
