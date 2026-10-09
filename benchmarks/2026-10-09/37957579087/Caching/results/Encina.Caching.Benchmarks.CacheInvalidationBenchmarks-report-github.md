```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **275.53 μs** |      **2.760 μs** |     **1.642 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    275.22 μs |     31.870 μs |     1.747 μs |     ? |       ? | 10.2539 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,325.88 μs** |     **14.209 μs** |     **8.455 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.18 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,318.11 μs |    120.829 μs |     6.623 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.08 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **25.52 μs** |      **0.433 μs** |     **0.287 μs** |  **1.00** |    **0.02** |  **1.0071** |  **0.0305** |       **-** |   **16.88 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     39.77 μs |      1.365 μs |     0.903 μs |  1.56 |    0.04 |  1.1597 |       - |       - |   19.23 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    263.44 μs |      1.716 μs |     1.135 μs | 10.32 |    0.12 | 10.2539 |       - |       - |  169.03 KB |       10.01 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     25.58 μs |      8.166 μs |     0.448 μs |  1.00 |    0.02 |  1.0071 |  0.0305 |       - |   16.84 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     39.60 μs |     21.372 μs |     1.171 μs |  1.55 |    0.05 |  1.1597 |       - |       - |   19.32 KB |        1.15 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    263.92 μs |     43.778 μs |     2.400 μs | 10.32 |    0.17 | 10.2539 |       - |       - |  169.05 KB |       10.04 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **4,192.78 μs** |  **1,966.252 μs** | **1,300.554 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |   **646.9 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  2,727.28 μs |  8,074.740 μs |   442.604 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  366.92 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **9,638.14 μs** |  **5,605.251 μs** | **3,707.527 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1274.88 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  5,621.10 μs | 18,558.670 μs | 1,017.263 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  714.87 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **14,465.63 μs** | **12,550.899 μs** | **7,468.839 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.87 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 14,699.90 μs | 41,029.080 μs | 2,248.942 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.82 KB |           ? |
