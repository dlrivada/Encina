```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **373.49 μs** |      **2.036 μs** |     **1.212 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    372.50 μs |     14.093 μs |     0.772 μs |     ? |       ? | 10.2539 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,907.04 μs** |      **9.986 μs** |     **6.605 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.19 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,866.88 μs |    117.990 μs |     6.467 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.06 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **36.31 μs** |      **0.123 μs** |     **0.081 μs** |  **1.00** |    **0.00** |  **0.9766** |       **-** |       **-** |   **16.89 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     64.85 μs |      7.436 μs |     4.918 μs |  1.79 |    0.13 |  1.0986 |       - |       - |   19.12 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    384.40 μs |      1.267 μs |     0.754 μs | 10.59 |    0.03 | 10.2539 |       - |       - |  169.04 KB |       10.01 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     36.55 μs |      1.634 μs |     0.090 μs |  1.00 |    0.00 |  0.9766 |       - |       - |   16.83 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     57.07 μs |     49.482 μs |     2.712 μs |  1.56 |    0.06 |  1.1597 |       - |       - |   19.16 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    377.50 μs |     24.398 μs |     1.337 μs | 10.33 |    0.04 | 10.2539 |       - |       - |  169.06 KB |       10.05 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **4,914.75 μs** |  **2,353.488 μs** | **1,556.687 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.93 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,200.81 μs |  8,706.656 μs |   477.241 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  373.11 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **4,940.30 μs** |  **2,554.757 μs** | **1,689.814 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |   **654.9 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,261.06 μs |  8,291.250 μs |   454.471 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.88 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **17,119.49 μs** |  **8,697.385 μs** | **5,752.782 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.85 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  9,745.84 μs | 45,236.809 μs | 2,479.582 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.82 KB |           ? |
