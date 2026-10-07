```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **337.59 μs** |      **1.915 μs** |     **1.266 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    339.64 μs |     10.955 μs |     0.600 μs |     ? |       ? | 10.2539 |       - |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,703.29 μs** |     **12.481 μs** |     **8.256 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.07 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,685.88 μs |     56.728 μs |     3.109 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.17 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **31.87 μs** |      **0.223 μs** |     **0.147 μs** |  **1.00** |    **0.01** |  **1.0376** |       **-** |       **-** |   **17.02 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     52.59 μs |      1.270 μs |     0.756 μs |  1.65 |    0.02 |  1.1597 |       - |       - |   19.21 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    343.52 μs |      1.786 μs |     1.181 μs | 10.78 |    0.06 | 10.2539 |       - |       - |  169.06 KB |        9.94 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     32.32 μs |      1.860 μs |     0.102 μs |  1.00 |    0.00 |  0.9766 |       - |       - |   16.89 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     51.76 μs |     18.869 μs |     1.034 μs |  1.60 |    0.03 |  1.1597 |       - |       - |   19.22 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    340.14 μs |     27.232 μs |     1.493 μs | 10.52 |    0.05 | 10.2539 |       - |       - |  169.04 KB |       10.01 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,414.89 μs** |  **2,514.399 μs** | **1,663.119 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.91 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,426.44 μs |  8,219.901 μs |   450.561 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 |  366.92 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **5,292.89 μs** |  **2,462.561 μs** | **1,628.832 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.88 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,600.33 μs | 10,767.671 μs |   590.212 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.87 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **18,808.75 μs** | **12,733.621 μs** | **8,422.502 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.86 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 10,807.43 μs | 41,937.668 μs | 2,298.745 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  908.81 KB |           ? |
