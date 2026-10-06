```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **381.33 μs** |      **0.828 μs** |     **0.433 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    381.30 μs |     16.156 μs |     0.886 μs |     ? |       ? | 10.2539 |       - |       - |  171.17 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,929.05 μs** |     **12.058 μs** |     **7.976 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.24 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,858.86 μs |    125.189 μs |     6.862 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.13 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **35.84 μs** |      **0.112 μs** |     **0.067 μs** |  **1.00** |    **0.00** |  **0.9766** |       **-** |       **-** |   **16.77 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     55.51 μs |      5.087 μs |     3.027 μs |  1.55 |    0.08 |  1.0986 |       - |       - |   19.45 KB |        1.16 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    377.37 μs |      0.608 μs |     0.362 μs | 10.53 |    0.02 | 10.2539 |       - |       - |  169.06 KB |       10.08 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     36.02 μs |      0.375 μs |     0.021 μs |  1.00 |    0.00 |  0.9766 |       - |       - |   16.88 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     56.39 μs |     49.782 μs |     2.729 μs |  1.57 |    0.07 |  1.1597 |       - |       - |   19.19 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    380.07 μs |     12.404 μs |     0.680 μs | 10.55 |    0.02 | 10.2539 |       - |       - |  169.05 KB |       10.01 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **4,840.17 μs** |  **2,235.512 μs** | **1,478.653 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.93 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,169.03 μs |  8,201.674 μs |   449.561 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |   366.9 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **4,808.08 μs** |  **2,181.743 μs** | **1,443.088 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.87 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,205.05 μs |  8,271.698 μs |   453.400 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.87 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **19,222.78 μs** | **12,169.278 μs** | **8,049.224 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.84 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  9,547.01 μs | 37,647.248 μs | 2,063.573 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  938.45 KB |           ? |
