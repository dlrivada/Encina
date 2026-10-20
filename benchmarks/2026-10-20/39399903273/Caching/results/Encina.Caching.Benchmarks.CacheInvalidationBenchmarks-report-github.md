```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **409.76 μs** |      **2.666 μs** |     **1.764 μs** |     **?** |       **?** |  **6.8359** |       **-** |       **-** |  **173.81 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    405.89 μs |      5.734 μs |     0.314 μs |     ? |       ? |  6.8359 |       - |       - |  173.82 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **2,026.55 μs** |      **5.120 μs** |     **3.047 μs** |     **?** |       **?** | **35.1563** |       **-** |       **-** |  **868.46 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  2,049.97 μs |     74.764 μs |     4.098 μs |     ? |       ? | 35.1563 |       - |       - |  868.44 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **38.69 μs** |      **0.303 μs** |     **0.159 μs** |  **1.00** |    **0.01** |  **0.6714** |       **-** |       **-** |   **17.31 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     55.97 μs |      7.744 μs |     5.122 μs |  1.45 |    0.13 |  0.7935 |  0.0610 |       - |   19.63 KB |        1.13 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    402.29 μs |      1.311 μs |     0.867 μs | 10.40 |    0.05 |  6.8359 |       - |       - |  171.71 KB |        9.92 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     38.21 μs |      1.894 μs |     0.104 μs |  1.00 |    0.00 |  0.6714 |       - |       - |   17.33 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     69.45 μs |    146.848 μs |     8.049 μs |  1.82 |    0.18 |  0.7324 |       - |       - |   19.43 KB |        1.12 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    405.93 μs |     48.296 μs |     2.647 μs | 10.62 |    0.07 |  6.8359 |       - |       - |  171.71 KB |        9.91 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,918.93 μs** |  **3,085.825 μs** | **2,041.082 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **647.22 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  3,667.21 μs | 11,336.212 μs |   621.376 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  367.21 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **6,698.01 μs** |  **4,062.177 μs** | **2,686.878 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **655.12 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  3,985.67 μs | 14,105.954 μs |   773.195 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  375.15 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **24,245.59 μs** | **12,008.785 μs** | **7,943.068 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **1609.12 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       | 15,598.28 μs | 53,828.513 μs | 2,950.522 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  909.12 KB |           ? |
