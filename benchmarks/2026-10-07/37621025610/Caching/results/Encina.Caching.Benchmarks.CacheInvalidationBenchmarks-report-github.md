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
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **384.33 μs** |      **2.445 μs** |     **1.617 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    381.91 μs |     68.861 μs |     3.775 μs |     ? |       ? | 10.2539 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,890.96 μs** |      **6.675 μs** |     **3.972 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.12 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,890.27 μs |     39.736 μs |     2.178 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.06 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **35.33 μs** |      **0.158 μs** |     **0.083 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |   **17.16 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     61.57 μs |      3.175 μs |     2.100 μs |  1.74 |    0.06 |  1.1597 |       - |       - |   19.16 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    384.28 μs |      1.230 μs |     0.814 μs | 10.88 |    0.03 | 10.2539 |       - |       - |  169.03 KB |        9.85 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     35.24 μs |      1.570 μs |     0.086 μs |  1.00 |    0.00 |  1.0376 |       - |       - |   16.98 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     56.99 μs |     79.158 μs |     4.339 μs |  1.62 |    0.11 |  1.1597 |       - |       - |   19.16 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    379.88 μs |      3.998 μs |     0.219 μs | 10.78 |    0.02 | 10.2539 |       - |       - |  169.05 KB |        9.95 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **4,921.66 μs** |  **2,324.428 μs** | **1,537.465 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.96 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,237.87 μs |  8,415.175 μs |   461.264 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.87 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **4,893.72 μs** |  **2,272.386 μs** | **1,503.043 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.85 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,086.84 μs |  7,319.705 μs |   401.218 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.86 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **18,310.02 μs** | **12,117.395 μs** | **8,014.906 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.86 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  8,450.97 μs | 30,186.083 μs | 1,654.601 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.84 KB |           ? |
