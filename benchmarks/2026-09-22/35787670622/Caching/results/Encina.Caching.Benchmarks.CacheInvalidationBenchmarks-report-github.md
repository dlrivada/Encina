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
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **326.27 μs** |      **2.050 μs** |     **1.356 μs** |     **?** |       **?** |  **1.9531** |       **-** |       **-** |  **171.15 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    326.18 μs |     40.164 μs |     2.202 μs |     ? |       ? |  1.9531 |       - |       - |  171.17 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |  **1,588.10 μs** |      **2.067 μs** |     **1.230 μs** |     **?** |       **?** |  **9.7656** |       **-** |       **-** |   **855.1 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |  1,598.57 μs |    217.384 μs |    11.916 μs |     ? |       ? |  9.7656 |       - |       - |  855.11 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **31.25 μs** |      **0.122 μs** |     **0.064 μs** |  **1.00** |    **0.00** |  **0.1831** |       **-** |       **-** |   **16.77 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     46.35 μs |      3.375 μs |     1.765 μs |  1.48 |    0.05 |  0.1831 |       - |       - |   19.19 KB |        1.14 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    327.32 μs |      1.654 μs |     1.094 μs | 10.47 |    0.04 |  1.9531 |       - |       - |  169.06 KB |       10.08 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     30.70 μs |      7.398 μs |     0.406 μs |  1.00 |    0.02 |  0.1831 |       - |       - |   16.83 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     43.30 μs |     34.779 μs |     1.906 μs |  1.41 |    0.06 |  0.1831 |       - |       - |   19.22 KB |        1.14 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    324.04 μs |     14.620 μs |     0.801 μs | 10.55 |    0.12 |  1.9531 |       - |       - |  169.06 KB |       10.05 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **4,337.21 μs** |  **1,838.063 μs** | **1,215.765 μs** |     **?** |       **?** | **14.6484** | **14.6484** | **14.6484** |  **646.91 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  2,974.46 μs |  6,792.875 μs |   372.340 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  366.92 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       |  **4,290.66 μs** |  **1,773.517 μs** | **1,173.072 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **654.85 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  2,979.81 μs |  6,689.902 μs |   366.696 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  374.86 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **11,498.21 μs** |  **5,673.437 μs** | **3,752.627 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **1608.85 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  7,147.07 μs | 17,836.104 μs |   977.657 μs |     ? |       ? | 15.6250 | 15.6250 | 15.6250 |  908.83 KB |           ? |
