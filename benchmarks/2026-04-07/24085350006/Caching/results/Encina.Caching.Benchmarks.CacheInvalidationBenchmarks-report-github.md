```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **344.56 μs** |      **1.042 μs** |      **0.545 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    345.77 μs |     23.575 μs |      1.292 μs |     ? |       ? | 10.2539 |       - |       - |  171.15 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,750.75 μs** |     **13.758 μs** |      **9.100 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.12 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,719.86 μs |     49.194 μs |      2.697 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.19 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **31.72 μs** |      **0.202 μs** |      **0.134 μs** |  **1.00** |    **0.01** |  **1.0376** |       **-** |       **-** |   **17.08 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     51.07 μs |      1.513 μs |      0.900 μs |  1.61 |    0.03 |  1.1597 |       - |       - |   19.42 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    346.44 μs |      1.781 μs |      1.178 μs | 10.92 |    0.06 | 10.2539 |       - |       - |  169.05 KB |        9.90 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     33.08 μs |      3.350 μs |      0.184 μs |  1.00 |    0.01 |  0.9766 |       - |       - |   16.72 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     52.85 μs |     12.163 μs |      0.667 μs |  1.60 |    0.02 |  1.1597 |       - |       - |   19.33 KB |        1.16 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    346.15 μs |     30.699 μs |      1.683 μs | 10.47 |    0.07 | 10.2539 |       - |       - |  169.04 KB |       10.11 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,576.83 μs** |  **2,669.561 μs** |  **1,765.750 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |   **646.9 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,178.76 μs |  8,824.315 μs |    483.690 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.91 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **7,050.02 μs** |  **4,262.763 μs** |  **2,819.554 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.85 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,575.00 μs | 10,114.267 μs |    554.397 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.87 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **26,257.45 μs** | **20,582.010 μs** | **13,613.725 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.84 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 15,014.13 μs | 36,810.387 μs |  2,017.702 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.83 KB |           ? |
