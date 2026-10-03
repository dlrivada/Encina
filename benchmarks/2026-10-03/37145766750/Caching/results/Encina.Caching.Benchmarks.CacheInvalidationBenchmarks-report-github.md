```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **344.62 μs** |      **2.793 μs** |     **1.847 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    342.02 μs |     28.297 μs |     1.551 μs |     ? |       ? | 10.2539 |       - |       - |  171.13 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,732.22 μs** |      **9.334 μs** |     **6.174 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,713.97 μs |     64.412 μs |     3.531 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.07 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **32.22 μs** |      **0.172 μs** |     **0.114 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |   **17.05 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     52.30 μs |      1.943 μs |     1.285 μs |  1.62 |    0.04 |  1.1597 |       - |       - |   19.18 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    339.20 μs |      2.489 μs |     1.646 μs | 10.53 |    0.06 | 10.2539 |       - |       - |  169.05 KB |        9.91 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     32.35 μs |      2.728 μs |     0.150 μs |  1.00 |    0.01 |  1.0376 |       - |       - |   16.98 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     50.45 μs |     33.911 μs |     1.859 μs |  1.56 |    0.05 |  1.1597 |       - |       - |   19.49 KB |        1.15 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    341.68 μs |     27.537 μs |     1.509 μs | 10.56 |    0.06 | 10.2539 |       - |       - |  169.05 KB |        9.95 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,594.36 μs** |  **2,690.844 μs** | **1,779.827 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.91 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,266.83 μs | 10,287.399 μs |   563.887 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  366.91 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,500.86 μs** |  **2,889.692 μs** | **1,911.352 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.87 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,522.33 μs |  9,418.252 μs |   516.246 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.88 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **20,927.42 μs** | **13,805.584 μs** | **9,131.539 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.88 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 10,955.69 μs | 39,323.630 μs | 2,155.461 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.81 KB |           ? |
