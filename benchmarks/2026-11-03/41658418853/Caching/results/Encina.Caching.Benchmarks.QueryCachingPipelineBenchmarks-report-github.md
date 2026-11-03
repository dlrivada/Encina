```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **8.116 μs** |     **0.8379 μs** |  **0.4986 μs** |  **1.00** |    **0.08** |  **0.1526** | **0.0610** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   6.184 μs |     0.2427 μs |  0.1605 μs |  0.76 |    0.05 |  0.2060 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  87.679 μs |     6.9892 μs |  4.1592 μs | 10.84 |    0.78 |  2.1973 | 1.4648 |  43.25 KB |       17.25 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  29.074 μs |     0.7228 μs |  0.4781 μs |  3.59 |    0.21 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   7.603 μs |     1.7954 μs |  0.0984 μs |  1.00 |    0.02 |  0.1526 | 0.0610 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   6.426 μs |     1.0602 μs |  0.0581 μs |  0.85 |    0.01 |  0.2060 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  89.908 μs |   522.6258 μs | 28.6469 μs | 11.83 |    3.27 |  1.4648 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  30.583 μs |    17.8358 μs |  0.9776 μs |  4.02 |    0.12 |  0.9766 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **56.989 μs** |     **2.5242 μs** |  **1.3202 μs** |     **?** |       **?** |  **1.7700** | **0.8545** |  **31.84 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  60.292 μs |   203.1503 μs | 11.1354 μs |     ? |       ? |  1.3428 | 0.4272 |  22.39 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **294.927 μs** |    **32.7191 μs** | **17.1127 μs** |     **?** |       **?** |  **6.5918** | **2.1973** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 296.692 μs | 1,066.1859 μs | 58.4412 μs |     ? |       ? |  6.3477 | 1.9531 |  111.3 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **616.605 μs** |    **90.6949 μs** | **53.9711 μs** |     **?** |       **?** | **13.1836** | **4.3945** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 531.974 μs |    89.1912 μs |  4.8889 μs |     ? |       ? | 12.6953 | 3.9063 | 222.45 KB |           ? |
