```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                          | Job        | IterationCount | LaunchCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |----------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **10**               | **?**        |    **189.11 μs** |      **4.523 μs** |     **2.691 μs** |     **?** |       **?** | **10.2539** |  **0.2441** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 10               | ?        |    188.55 μs |     78.946 μs |     4.327 μs |     ? |       ? | 10.2539 |  0.2441 |       - |  171.16 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **?**        |    **926.42 μs** |     **18.147 μs** |    **12.003 μs** |     **?** |       **?** | **51.7578** |  **2.9297** |       **-** |  **855.17 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | ShortRun   | 3              | 1           | 50               | ?        |    957.87 μs |     97.580 μs |     5.349 μs |     ? |       ? | 51.7578 |  2.9297 |       - |  855.14 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **?**        |     **18.15 μs** |      **0.191 μs** |     **0.126 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |   **16.99 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | ?                | ?        |     30.08 μs |      1.445 μs |     0.956 μs |  1.66 |    0.05 |  1.1597 |  0.0305 |       - |   18.96 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | ?                | ?        |    186.10 μs |      1.743 μs |     1.037 μs | 10.26 |    0.09 | 10.2539 |  0.2441 |       - |  169.06 KB |        9.95 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | ShortRun   | 3              | 1           | ?                | ?        |     18.24 μs |      2.462 μs |     0.135 μs |  1.00 |    0.01 |  1.0376 |  0.0305 |       - |   17.05 KB |        1.00 |
| Invalidation_WithMatchingKeys   | ShortRun   | 3              | 1           | ?                | ?        |     29.31 μs |     22.843 μs |     1.252 μs |  1.61 |    0.06 |  1.1597 |  0.0305 |       - |   19.07 KB |        1.12 |
| Invalidation_SequentialCommands | ShortRun   | 3              | 1           | ?                | ?        |    188.19 μs |     42.238 μs |     2.315 μs | 10.32 |    0.13 | 10.2539 |  0.2441 |       - |  169.05 KB |        9.92 |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **5**        |  **5,658.63 μs** |  **5,553.726 μs** | **3,673.446 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.92 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 5        |  2,259.29 μs |  7,733.562 μs |   423.903 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  366.94 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **10**       | **17,411.16 μs** | **11,502.643 μs** | **7,608.286 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1266.79 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 10       |  4,945.03 μs | 23,574.866 μs | 1,292.218 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 |  706.83 KB |           ? |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **?**                | **25**       | **10,995.70 μs** |  **5,480.704 μs** | **3,261.479 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1588.75 KB** |           **?** |
|                                 |            |                |             |                  |          |              |               |              |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | ShortRun   | 3              | 1           | ?                | 25       |  6,395.61 μs | 37,635.817 μs | 2,062.946 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 |  888.69 KB |           ? |
