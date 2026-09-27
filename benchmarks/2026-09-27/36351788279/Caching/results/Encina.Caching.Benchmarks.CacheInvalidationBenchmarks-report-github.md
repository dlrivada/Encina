```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **380.38 μs** |      **0.772 μs** |      **0.459 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.17 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    381.22 μs |     17.843 μs |      0.978 μs |     ? |       ? | 10.2539 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,963.29 μs** |     **15.531 μs** |      **8.123 μs** |     **?** |       **?** | **50.7813** |       **-** |       **-** |  **855.05 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,906.09 μs |     17.255 μs |      0.946 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.18 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **36.44 μs** |      **0.210 μs** |      **0.139 μs** |  **1.00** |    **0.01** |  **0.9766** |       **-** |       **-** |   **16.87 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     55.84 μs |      4.132 μs |      2.459 μs |  1.53 |    0.06 |  1.1597 |       - |       - |   19.37 KB |        1.15 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    382.09 μs |      2.447 μs |      1.456 μs | 10.48 |    0.05 | 10.2539 |       - |       - |  169.05 KB |       10.02 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     37.38 μs |      3.750 μs |      0.206 μs |  1.00 |    0.01 |  0.9766 |       - |       - |   16.83 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     61.43 μs |    181.460 μs |      9.946 μs |  1.64 |    0.23 |  1.0986 |       - |       - |   18.91 KB |        1.12 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    384.24 μs |     36.554 μs |      2.004 μs | 10.28 |    0.07 | 10.2539 |       - |       - |  169.05 KB |       10.05 |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,767.26 μs** |  **3,335.750 μs** |  **2,206.392 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.92 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,224.24 μs |  8,395.166 μs |    460.167 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.91 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **4,953.55 μs** |  **2,268.151 μs** |  **1,500.242 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.86 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,186.88 μs |  8,888.493 μs |    487.208 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.88 KB |           ? |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **22,183.86 μs** | **16,060.888 μs** | **10,623.283 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.88 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 10,570.59 μs | 37,245.547 μs |  2,041.554 μs |     ? |       ? | 19.5313 | 17.5781 | 17.5781 |  937.98 KB |           ? |
