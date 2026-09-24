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
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **263.03 μs** |      **1.658 μs** |     **1.097 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    261.60 μs |     10.867 μs |     0.596 μs |     ? |       ? | 10.2539 |       - |       - |  171.14 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,327.35 μs** |      **6.468 μs** |     **3.849 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |   **855.1 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,337.25 μs |    441.949 μs |    24.225 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.17 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **24.93 μs** |      **0.095 μs** |     **0.063 μs** |  **1.00** |    **0.00** |  **1.0376** |  **0.0305** |       **-** |   **16.98 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     39.50 μs |      1.999 μs |     1.322 μs |  1.58 |    0.05 |  1.1597 |       - |       - |   19.35 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    262.70 μs |      1.736 μs |     1.148 μs | 10.54 |    0.05 | 10.2539 |       - |       - |  169.03 KB |        9.96 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     25.16 μs |      1.073 μs |     0.059 μs |  1.00 |    0.00 |  1.0071 |  0.0305 |       - |   16.84 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     41.19 μs |     41.715 μs |     2.287 μs |  1.64 |    0.08 |  1.1597 |       - |       - |   19.03 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    267.79 μs |     24.652 μs |     1.351 μs | 10.64 |    0.05 | 10.2539 |       - |       - |  169.05 KB |       10.04 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **4,054.43 μs** |  **1,885.362 μs** | **1,247.050 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.92 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  2,756.27 μs |  8,169.665 μs |   447.807 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  366.92 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       | **11,231.66 μs** |  **8,229.076 μs** | **5,443.024 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1274.87 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  7,735.91 μs | 39,589.141 μs | 2,170.014 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  714.86 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **17,199.77 μs** |  **7,657.973 μs** | **5,065.275 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.85 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  7,870.44 μs | 30,631.185 μs | 1,678.999 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.82 KB |           ? |
