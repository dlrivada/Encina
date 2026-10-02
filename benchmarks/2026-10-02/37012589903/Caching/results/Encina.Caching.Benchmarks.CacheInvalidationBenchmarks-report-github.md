```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **330.56 μs** |      **2.501 μs** |     **1.488 μs** |     **?** |       **?** |  **1.9531** |       **-** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    320.30 μs |     35.626 μs |     1.953 μs |     ? |       ? |  1.9531 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,652.03 μs** |     **23.093 μs** |    **15.275 μs** |     **?** |       **?** |  **9.7656** |       **-** |       **-** |  **855.06 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,598.70 μs |     88.424 μs |     4.847 μs |     ? |       ? |  9.7656 |       - |       - |  855.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **31.44 μs** |      **0.069 μs** |     **0.046 μs** |  **1.00** |    **0.00** |  **0.1831** |       **-** |       **-** |   **16.92 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     45.56 μs |      3.656 μs |     2.419 μs |  1.45 |    0.07 |  0.1831 |       - |       - |   19.19 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    324.08 μs |      1.934 μs |     1.279 μs | 10.31 |    0.04 |  1.9531 |       - |       - |  169.05 KB |        9.99 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     29.94 μs |      1.024 μs |     0.056 μs |  1.00 |    0.00 |  0.1831 |       - |       - |   17.15 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     57.24 μs |     40.918 μs |     2.243 μs |  1.91 |    0.06 |  0.1831 |       - |       - |   19.07 KB |        1.11 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    330.72 μs |     22.067 μs |     1.210 μs | 11.05 |    0.04 |  1.9531 |       - |       - |  169.04 KB |        9.86 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,139.61 μs** |  **2,799.692 μs** | **1,851.823 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **646.93 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,015.72 μs |  7,925.724 μs |   434.436 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  366.94 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,207.29 μs** |  **2,840.084 μs** | **1,878.540 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.87 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,237.02 μs |  9,292.498 μs |   509.353 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.87 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **16,853.69 μs** |  **8,444.694 μs** | **5,585.642 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **1608.88 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 10,652.60 μs | 33,403.176 μs | 1,830.941 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  908.85 KB |           ? |
