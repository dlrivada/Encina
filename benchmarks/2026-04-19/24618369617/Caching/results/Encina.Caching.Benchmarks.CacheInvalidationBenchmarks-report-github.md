```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4


```
| Method                          | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | keyCount | Mean         | Error         | StdDev        | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |--------------- |------------ |------------ |----------------- |--------- |-------------:|--------------:|--------------:|------:|--------:|--------:|--------:|--------:|-----------:|------------:|
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               | **?**        |    **267.23 μs** |      **2.092 μs** |      **1.384 μs** |     **?** |       **?** | **10.2539** |       **-** |       **-** |  **171.16 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | 10               | ?        |    265.11 μs |      0.756 μs |      1.060 μs |     ? |       ? | 10.2539 |       - |       - |  171.15 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_ConcurrentCommands** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **?**        |  **1,337.74 μs** |      **6.645 μs** |      **3.955 μs** |     **?** |       **?** | **50.7813** |  **1.9531** |       **-** |  **855.14 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_ConcurrentCommands | MediumRun  | 15             | 2           | 10          | 50               | ?        |  1,328.57 μs |      9.040 μs |     12.673 μs |     ? |       ? | 50.7813 |  1.9531 |       - |  855.15 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_NoMatchingKeys**     | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **?**        |     **24.88 μs** |      **0.095 μs** |      **0.063 μs** |  **1.00** |    **0.00** |  **1.0376** |  **0.0305** |       **-** |   **17.15 KB** |        **1.00** |
| Invalidation_WithMatchingKeys   | Job-YFEFPZ | 10             | Default     | 3           | ?                | ?        |     39.88 μs |      1.743 μs |      1.153 μs |  1.60 |    0.04 |  1.1597 |       - |       - |   19.16 KB |        1.12 |
| Invalidation_SequentialCommands | Job-YFEFPZ | 10             | Default     | 3           | ?                | ?        |    263.51 μs |      1.581 μs |      1.046 μs | 10.59 |    0.05 | 10.2539 |       - |       - |  169.04 KB |        9.86 |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_NoMatchingKeys     | MediumRun  | 15             | 2           | 10          | ?                | ?        |     24.82 μs |      0.220 μs |      0.329 μs |  1.00 |    0.02 |  1.0376 |  0.0305 |       - |   17.02 KB |        1.00 |
| Invalidation_WithMatchingKeys   | MediumRun  | 15             | 2           | 10          | ?                | ?        |     38.95 μs |      0.739 μs |      1.107 μs |  1.57 |    0.05 |  1.1597 |       - |       - |   19.42 KB |        1.14 |
| Invalidation_SequentialCommands | MediumRun  | 15             | 2           | 10          | ?                | ?        |    266.04 μs |      1.170 μs |      1.678 μs | 10.72 |    0.15 | 10.2539 |       - |       - |  169.06 KB |        9.94 |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **5**        |  **5,147.56 μs** |  **3,681.353 μs** |  **2,434.987 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **646.93 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 5        | 10,083.73 μs |  2,003.358 μs |  2,998.532 μs |     ? |       ? | 16.6016 | 15.6250 | 15.6250 | 1126.91 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **10**       | **11,535.69 μs** |  **7,693.089 μs** |  **5,088.502 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **1274.87 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 10       | 23,446.02 μs |  5,381.228 μs |  8,054.369 μs |     ? |       ? | 17.5781 | 16.6016 | 16.6016 | 1040.34 KB |           ? |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| **Invalidation_MultipleKeys**       | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                | **25**       | **27,921.78 μs** | **17,396.909 μs** | **11,506.978 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **1608.82 KB** |           **?** |
|                                 |            |                |             |             |                  |          |              |               |               |       |         |         |         |         |            |             |
| Invalidation_MultipleKeys       | MediumRun  | 15             | 2           | 10          | ?                | 25       | 42,971.01 μs |  9,490.606 μs | 14,205.093 μs |     ? |       ? | 17.5781 | 15.6250 | 15.6250 | 1527.99 KB |           ? |
