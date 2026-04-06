```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | keyCount | concurrencyLevel | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |--------- |----------------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**        | **?**                |     **34.58 μs** |      **0.191 μs** |     **0.114 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |   **17.25 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?        | ?                |     59.11 μs |      6.243 μs |     4.129 μs |  1.71 |    0.11 |  1.1597 |       - |       - |   19.43 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?        | ?                |    388.41 μs |      2.402 μs |     1.589 μs | 11.23 |    0.06 | 10.2539 |       - |       - |  169.05 KB |        9.80 |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?        | ?                |     36.23 μs |      1.021 μs |     0.056 μs |  1.00 |    0.00 |  0.9766 |       - |       - |   16.88 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?        | ?                |     57.60 μs |     53.103 μs |     2.911 μs |  1.59 |    0.07 |  1.1597 |       - |       - |   19.16 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?        | ?                |    386.54 μs |     13.223 μs |     0.725 μs | 10.67 |    0.02 | 10.2539 |       - |       - |  169.04 KB |       10.02 |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **5**        | **?**                |  **5,392.15 μs** |  **2,956.655 μs** | **1,955.644 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.93 KB** |           **?** |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | 5        | ?                |  3,195.99 μs |  8,236.564 μs |   451.474 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.91 KB |           ? |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **?**        | **10**               |    **388.24 μs** |      **1.173 μs** |     **0.698 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | ?        | 10               |    378.78 μs |     20.391 μs |     1.118 μs |     ? |       ? | 10.2539 |       - |       - |  171.15 KB |           ? |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **10**       | **?**                |  **4,917.80 μs** |  **2,152.641 μs** | **1,423.839 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.88 KB** |           **?** |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | 10       | ?                |  3,155.26 μs |  8,067.629 μs |   442.214 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.88 KB |           ? |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **25**       | **?**                | **17,835.42 μs** | **12,575.884 μs** | **8,318.169 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.88 KB** |           **?** |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | 25       | ?                |  8,722.36 μs | 34,313.912 μs | 1,880.861 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.81 KB |           ? |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **?**        | **50**               |  **1,877.82 μs** |      **7.696 μs** |     **5.091 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.13 KB** |           **?** |
|                                 |            |                |             |          |                  |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | ?        | 50               |  1,924.85 μs |     45.952 μs |     2.519 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.14 KB |           ? |
