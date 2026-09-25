```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **377.52 μs** |      **2.440 μs** |     **1.614 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    379.58 μs |      6.527 μs |     0.358 μs |     ? |       ? | 10.2539 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,938.36 μs** |      **6.713 μs** |     **4.440 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.28 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,884.52 μs |     79.588 μs |     4.362 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.19 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **35.67 μs** |      **0.341 μs** |     **0.203 μs** |  **1.00** |    **0.01** |  **0.9766** |       **-** |       **-** |   **16.88 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     58.40 μs |      6.748 μs |     4.463 μs |  1.64 |    0.12 |  1.0986 |       - |       - |   19.42 KB |        1.15 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    375.95 μs |      1.316 μs |     0.783 μs | 10.54 |    0.06 | 10.2539 |       - |       - |  169.08 KB |       10.02 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     35.08 μs |      1.630 μs |     0.089 μs |  1.00 |    0.00 |  1.0376 |       - |       - |   17.11 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     58.57 μs |    125.421 μs |     6.875 μs |  1.67 |    0.17 |  1.0986 |       - |       - |   19.15 KB |        1.12 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    376.75 μs |     54.280 μs |     2.975 μs | 10.74 |    0.08 | 10.2539 |       - |       - |  169.05 KB |        9.88 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,647.55 μs** |  **3,114.172 μs** | **2,059.832 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.92 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,165.17 μs |  5,848.652 μs |   320.584 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.94 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **4,780.89 μs** |  **2,130.659 μs** | **1,409.299 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.84 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,138.21 μs |  5,620.406 μs |   308.073 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.86 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **17,024.97 μs** | **12,646.931 μs** | **8,365.162 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.86 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 10,194.57 μs | 61,740.362 μs | 3,384.198 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.85 KB |           ? |
