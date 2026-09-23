```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **388.26 μs** |      **0.737 μs** |      **0.488 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    383.45 μs |      7.823 μs |      0.429 μs |     ? |       ? | 10.2539 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,924.89 μs** |      **3.887 μs** |      **2.033 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.18 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,943.49 μs |      8.498 μs |      0.466 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.12 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **36.18 μs** |      **0.156 μs** |      **0.103 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |    **17.2 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     63.84 μs |      5.483 μs |      3.627 μs |  1.76 |    0.10 |  1.1597 |       - |       - |    19.2 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    386.50 μs |      0.893 μs |      0.467 μs | 10.68 |    0.03 | 10.2539 |       - |       - |  169.04 KB |        9.83 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     35.55 μs |      2.221 μs |      0.122 μs |  1.00 |    0.00 |  1.0376 |       - |       - |   17.09 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     66.67 μs |     73.538 μs |      4.031 μs |  1.88 |    0.10 |  1.0986 |       - |       - |   18.91 KB |        1.11 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    379.32 μs |     22.237 μs |      1.219 μs | 10.67 |    0.04 | 10.2539 |       - |       - |  169.05 KB |        9.89 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        | **10,871.37 μs** |  **6,122.573 μs** |  **4,049.703 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** |  **646.97 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  4,599.06 μs | 21,511.444 μs |  1,179.115 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |   366.9 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **9,084.76 μs** |  **5,721.757 μs** |  **3,784.588 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.89 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  4,589.44 μs | 25,573.065 μs |  1,401.746 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.86 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **29,552.93 μs** | **15,359.703 μs** | **10,159.492 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.89 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 18,714.33 μs | 55,916.514 μs |  3,064.973 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.86 KB |           ? |
