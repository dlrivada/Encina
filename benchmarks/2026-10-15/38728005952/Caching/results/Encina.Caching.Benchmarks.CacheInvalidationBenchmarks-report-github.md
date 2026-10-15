```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **382.23 μs** |      **1.121 μs** |     **0.586 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    378.01 μs |    108.861 μs |     5.967 μs |     ? |       ? | 10.2539 |       - |       - |  171.17 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,911.55 μs** |      **8.475 μs** |     **5.043 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,891.74 μs |     73.326 μs |     4.019 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.22 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **36.61 μs** |      **0.165 μs** |     **0.098 μs** |  **1.00** |    **0.00** |  **0.9766** |       **-** |       **-** |   **16.87 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     59.22 μs |     10.469 μs |     6.925 μs |  1.62 |    0.18 |  1.0986 |       - |       - |   19.21 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    370.74 μs |      1.067 μs |     0.558 μs | 10.13 |    0.03 | 10.2539 |       - |       - |  169.05 KB |       10.02 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     35.81 μs |      1.449 μs |     0.079 μs |  1.00 |    0.00 |  0.9766 |       - |       - |   16.91 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     67.01 μs |     60.191 μs |     3.299 μs |  1.87 |    0.08 |  1.0986 |       - |       - |   19.09 KB |        1.13 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    386.92 μs |     32.853 μs |     1.801 μs | 10.80 |    0.05 | 10.2539 |       - |       - |  169.04 KB |        9.99 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,410.13 μs** |  **2,606.360 μs** | **1,723.945 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.92 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,268.09 μs |  8,911.807 μs |   488.486 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  366.92 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,257.28 μs** |  **2,406.400 μs** | **1,591.685 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.87 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,318.73 μs |  7,572.482 μs |   415.073 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.84 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **19,614.11 μs** | **14,138.744 μs** | **9,351.904 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.84 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 10,271.88 μs | 47,780.915 μs | 2,619.033 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.85 KB |           ? |
