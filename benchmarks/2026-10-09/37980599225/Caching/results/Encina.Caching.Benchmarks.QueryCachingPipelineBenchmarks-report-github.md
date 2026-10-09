```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean       | Error         | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |-----------:|--------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |   **6.984 μs** |     **1.6927 μs** |  **1.0073 μs** |  **1.02** |    **0.20** |  **0.1144** | **0.0534** |   **1.98 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |   4.458 μs |     0.0802 μs |  0.0530 μs |  0.65 |    0.09 |  0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |  63.939 μs |     7.4039 μs |  4.4059 μs |  9.33 |    1.44 |  1.9531 | 1.4648 |  37.76 KB |       19.10 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |  21.580 μs |     0.4712 μs |  0.2465 μs |  3.15 |    0.44 |  0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |   6.266 μs |     3.7026 μs |  0.2029 μs |  1.00 |    0.04 |  0.1144 | 0.0534 |   1.98 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |   4.372 μs |     0.4099 μs |  0.0225 μs |  0.70 |    0.02 |  0.1526 |      - |   2.55 KB |        1.29 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |  69.933 μs |   439.4224 μs | 24.0862 μs | 11.17 |    3.35 |  1.0986 | 0.4883 |  18.99 KB |        9.61 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |  20.806 μs |     1.7589 μs |  0.0964 μs |  3.32 |    0.09 |  0.7324 |      - |  12.19 KB |        6.17 |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |  **44.278 μs** |     **2.1902 μs** |  **1.3033 μs** |     **?** |       **?** |  **1.4648** | **0.7324** |  **27.07 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |  46.503 μs |   200.2670 μs | 10.9773 μs |     ? |       ? |  1.0376 | 0.3662 |  17.62 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               | **223.970 μs** |    **44.7645 μs** | **26.6386 μs** |     **?** |       **?** |  **5.1270** | **1.7090** |  **87.47 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               | 242.006 μs |   562.6029 μs | 30.8382 μs |     ? |       ? |  5.1270 | 1.7090 |  87.47 KB |           ? |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **431.739 μs** |    **44.1654 μs** | **23.0993 μs** |     **?** |       **?** | **10.2539** | **3.4180** | **174.78 KB** |           **?** |
|                                     |            |                |             |                  |            |               |            |       |         |         |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 470.010 μs | 1,233.5964 μs | 67.6176 μs |     ? |       ? | 10.2539 | 3.4180 | 174.78 KB |           ? |
