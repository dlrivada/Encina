```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | concurrencyLevel | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |----------------- |-----------:|-----------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**                |   **8.546 μs** |  **1.4873 μs** |  **0.8851 μs** |  **1.01** |    **0.14** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | 3           | ?                |   5.820 μs |  0.0741 μs |  0.0388 μs |  0.69 |    0.07 |  0.2060 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | 3           | ?                |  86.022 μs |  5.9569 μs |  3.5449 μs | 10.16 |    1.08 |  2.1973 | 1.4648 |  43.25 KB |       17.25 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | 3           | ?                |  29.833 μs |  0.5435 μs |  0.3595 μs |  3.52 |    0.35 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |             |                  |            |            |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | MediumRun  | 15             | 2           | 10          | ?                |   9.099 μs |  0.3635 μs |  0.5213 μs |  1.00 |    0.08 |  0.1526 | 0.0610 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | MediumRun  | 15             | 2           | 10          | ?                |   5.834 μs |  0.0899 μs |  0.1345 μs |  0.64 |    0.04 |  0.2060 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | MediumRun  | 15             | 2           | 10          | ?                |  83.950 μs |  3.8264 μs |  5.4878 μs |  9.26 |    0.79 |  1.4648 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | MediumRun  | 15             | 2           | 10          | ?                |  29.401 μs |  0.1708 μs |  0.2338 μs |  3.24 |    0.18 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |             |                  |            |            |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**               |  **57.843 μs** |  **4.3359 μs** |  **2.5802 μs** |     **?** |       **?** |  **1.7700** | **0.9155** |  **31.68 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 10               |  60.656 μs |  2.0604 μs |  2.9549 μs |     ? |       ? |  1.3428 | 0.4272 |  22.39 KB |           ? |
|                                     |            |                |             |             |                  |            |            |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **50**               | **280.286 μs** | **45.8847 μs** | **27.3052 μs** |     **?** |       **?** |  **6.3477** | **1.9531** |  **111.3 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 50               | 293.458 μs | 10.3691 μs | 14.5360 μs |     ? |       ? |  6.5918 | 2.1973 |  111.3 KB |           ? |
|                                     |            |                |             |             |                  |            |            |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **100**              | **556.090 μs** | **43.4098 μs** | **25.8324 μs** |     **?** |       **?** | **12.6953** | **3.9063** | **222.45 KB** |           **?** |
|                                     |            |                |             |             |                  |            |            |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | MediumRun  | 15             | 2           | 10          | 100              | 564.828 μs | 11.8067 μs | 15.7615 μs |     ? |       ? | 12.6953 | 3.9063 | 222.45 KB |           ? |
