```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                              | Job        | IterationCount | LaunchCount | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |----------------- |------------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Pipeline_CacheMiss**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**                |    **13.52 μs** |   **0.528 μs** |  **0.314 μs** |  **1.00** |    **0.03** | **0.0916** | **0.0305** |   **2.51 KB** |        **1.00** |
| Pipeline_CacheHit                   | Job-YFEFPZ | 10             | Default     | ?                |    14.88 μs |   0.035 μs |  0.021 μs |  1.10 |    0.02 | 0.1373 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | Job-YFEFPZ | 10             | Default     | ?                |   141.22 μs |   5.720 μs |  2.991 μs | 10.45 |    0.31 | 0.9766 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | Job-YFEFPZ | 10             | Default     | ?                |    72.80 μs |   0.364 μs |  0.241 μs |  5.39 |    0.12 | 0.6104 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_CacheMiss                  | ShortRun   | 3              | 1           | ?                |    13.18 μs |   2.355 μs |  0.129 μs |  1.00 |    0.01 | 0.0916 | 0.0305 |   2.51 KB |        1.00 |
| Pipeline_CacheHit                   | ShortRun   | 3              | 1           | ?                |    13.92 μs |   0.331 μs |  0.018 μs |  1.06 |    0.01 | 0.1373 |      - |    3.4 KB |        1.36 |
| Pipeline_SequentialDifferentQueries | ShortRun   | 3              | 1           | ?                |   144.11 μs | 396.403 μs | 21.728 μs | 10.93 |    1.43 | 0.9766 | 0.7324 |   24.3 KB |        9.69 |
| Pipeline_SequentialSameQuery        | ShortRun   | 3              | 1           | ?                |    68.61 μs |  14.563 μs |  0.798 μs |  5.21 |    0.07 | 0.6104 |      - |  16.41 KB |        6.54 |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**               |   **107.92 μs** |   **2.050 μs** |  **1.072 μs** |     **?** |       **?** | **0.8545** | **0.3662** |  **22.39 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 10               |   116.56 μs | 235.181 μs | 12.891 μs |     ? |       ? | 0.8545 | 0.3662 |  22.39 KB |           ? |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **50**               |   **518.88 μs** |   **7.479 μs** |  **3.912 μs** |     **?** |       **?** | **3.9063** | **0.9766** |  **111.3 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 50               |   511.99 μs |  58.847 μs |  3.226 μs |     ? |       ? | 3.9063 | 0.9766 |  111.3 KB |           ? |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| **Pipeline_ConcurrentAccess**           | **Job-YFEFPZ** | **10**             | **Default**     | **100**              | **1,040.48 μs** |   **8.049 μs** |  **4.210 μs** |     **?** |       **?** | **7.8125** | **1.9531** | **222.45 KB** |           **?** |
|                                     |            |                |             |                  |             |            |           |       |         |        |        |           |             |
| Pipeline_ConcurrentAccess           | ShortRun   | 3              | 1           | 100              | 1,021.81 μs | 200.622 μs | 10.997 μs |     ? |       ? | 7.8125 | 1.9531 | 222.45 KB |           ? |
